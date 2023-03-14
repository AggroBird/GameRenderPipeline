using UnityEngine;
using UnityEngine.Rendering;

namespace AggroBird.GRP
{
    internal sealed class Shadows
    {
        private const string bufferName = "Shadows";
        private CommandBuffer buffer = new CommandBuffer { name = bufferName };

        private const int
            MaxShadowedDirectionalLightCount = 4,
            MaxShadowedOtherLightCount = 16,
            MaxCascades = 4;

        struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        struct ShadowedOtherLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
            public bool isPoint;
        }

        private static readonly int
            directionalShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
            directionalShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices"),
            otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas"),
            otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices"),
            otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles"),
            cascadeCountId = Shader.PropertyToID("_CascadeCount"),
            cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres"),
            cascadeDataId = Shader.PropertyToID("_CascadeData"),
            shadowAtlastSizeId = Shader.PropertyToID("_ShadowAtlasSize"),
            shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade"),
            shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");

        private ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
        private ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[MaxShadowedOtherLightCount];

        private static Matrix4x4[]
            directionalShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascades],
            otherShadowMatrices = new Matrix4x4[MaxShadowedOtherLightCount];

        private static Vector4[]
            cascadeCullingSpheres = new Vector4[MaxCascades],
            cascadeData = new Vector4[MaxCascades],
            otherShadowTiles = new Vector4[MaxShadowedOtherLightCount];

        private int
            shadowedDirectionalLightCount,
            shadowedOtherLightCount;

        internal static readonly GlobalKeyword[] directionalFilterKeywords =
        {
            GlobalKeyword.Create("_DIRECTIONAL_PCF3"),
            GlobalKeyword.Create("_DIRECTIONAL_PCF5"),
            GlobalKeyword.Create("_DIRECTIONAL_PCF7"),
        };

        internal static readonly GlobalKeyword[] otherFilterKeywords =
        {
            GlobalKeyword.Create("_OTHER_PCF3"),
            GlobalKeyword.Create("_OTHER_PCF5"),
            GlobalKeyword.Create("_OTHER_PCF7"),
        };

        internal static readonly GlobalKeyword[] cascadeBlendKeywords =
        {
            GlobalKeyword.Create("_CASCADE_BLEND_SOFT"),
            GlobalKeyword.Create("_CASCADE_BLEND_DITHER"),
        };


        private Vector4 atlasSizes;

        private ScriptableRenderContext context;
        private CullingResults cullingResults;

        private ShadowSettings settings;


        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
        {
            this.context = context;
            this.cullingResults = cullingResults;
            this.settings = settings;

            shadowedDirectionalLightCount = shadowedOtherLightCount = 0;
        }

        public void Cleanup()
        {
            if (shadowedDirectionalLightCount > 0)
            {
                buffer.ReleaseTemporaryRT(directionalShadowAtlasId);
                ExecuteBuffer();
            }
            if (shadowedOtherLightCount > 0)
            {
                buffer.ReleaseTemporaryRT(otherShadowAtlasId);
                ExecuteBuffer();
            }
        }

        public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                if (shadowedDirectionalLightCount < MaxShadowedDirectionalLightCount)
                {
                    if (cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                    {
                        shadowedDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight
                        {
                            visibleLightIndex = visibleLightIndex,
                            slopeScaleBias = light.shadowBias,
                            nearPlaneOffset = light.shadowNearPlane
                        };

                        return new Vector4(light.shadowStrength, settings.directional.cascadeCount * shadowedDirectionalLightCount++, light.shadowNormalBias, -1f);
                    }
                }
            }
            return new Vector4(0f, 0f, 0f, -1f);
        }
        public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
        {
            if (light.shadows != LightShadows.None && light.shadowStrength > 0f)
            {
                bool isPoint = light.type == LightType.Point;
                int newLightCount = (isPoint ? 6 : 1);
                if (shadowedOtherLightCount + newLightCount < MaxShadowedOtherLightCount)
                {
                    if (cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
                    {
                        shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight
                        {
                            visibleLightIndex = visibleLightIndex,
                            slopeScaleBias = light.shadowBias,
                            normalBias = light.shadowNormalBias,
                            isPoint = isPoint
                        };

                        Vector4 data = new Vector4(light.shadowStrength, shadowedOtherLightCount, isPoint ? 1f : 0f, -1);
                        shadowedOtherLightCount += newLightCount;
                        return data;
                    }
                }
            }

            return new Vector4(0f, 0f, 0f, -1f);
        }

        public void Render()
        {
            if (shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            if (shadowedOtherLightCount > 0)
            {
                RenderOtherShadows();
            }

            buffer.BeginSample(bufferName);
            buffer.SetGlobalInt(cascadeCountId, shadowedDirectionalLightCount > 0 ? settings.directional.cascadeCount : 0);
            float f = 1f - settings.directional.cascadeFade;
            buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));

            buffer.SetGlobalVector(shadowAtlastSizeId, atlasSizes);
            buffer.EndSample(bufferName);
            ExecuteBuffer();
        }

        private void RenderDirectionalShadows()
        {
            int atlasSize = (int)settings.directional.atlasSize;
            atlasSizes.x = atlasSize;
            atlasSizes.y = 1f / atlasSize;

            buffer.GetTemporaryRT(directionalShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            buffer.SetRenderTarget(directionalShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(true, false, Color.clear);
            buffer.SetGlobalBool(shadowPancakingId, true);
            buffer.BeginSample(bufferName);
            ExecuteBuffer();

            int tiles = shadowedDirectionalLightCount * settings.directional.cascadeCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }

            buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
            buffer.SetGlobalMatrixArray(directionalShadowMatricesId, directionalShadowMatrices);

            buffer.SetKeywords(directionalFilterKeywords, (int)settings.directional.filter - 1);
            buffer.SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);

            buffer.EndSample(bufferName);
            ExecuteBuffer();
        }
        private void RenderDirectionalShadows(int index, int split, int tileSize)
        {
            ShadowedDirectionalLight light = shadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
            int cascadeCount = settings.directional.cascadeCount;
            int tileOffset = index * cascadeCount;
            Vector3 ratios = settings.directional.CascadeRatios;

            float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);

            float tileScale = 1f / split;
            for (int i = 0; i < cascadeCount; i++)
            {
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;
                if (index == 0)
                {
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }

                int tileIndex = tileOffset + i;
                directionalShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), tileScale);

                buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
                ExecuteBuffer();
                context.DrawShadows(ref shadowSettings);
                buffer.SetGlobalDepthBias(0f, 0f);
            }
        }
        private void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            float filterSize = texelSize * ((float)settings.directional.filter + 1f);
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;
            cascadeData[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
        }

        private void RenderOtherShadows()
        {
            int atlasSize = (int)settings.other.atlasSize;
            atlasSizes.z = atlasSize;
            atlasSizes.w = 1f / atlasSize;

            buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.ClearRenderTarget(true, false, Color.clear);
            buffer.SetGlobalBool(shadowPancakingId, false);
            buffer.BeginSample(bufferName);
            ExecuteBuffer();

            int tiles = shadowedOtherLightCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;
            for (int i = 0; i < shadowedOtherLightCount;)
            {
                if (shadowedOtherLights[i].isPoint)
                {
                    RenderPointShadows(i, split, tileSize);
                    i += 6;
                }
                else
                {
                    RenderSpotShadows(i, split, tileSize);
                    i += 1;
                }
            }

            buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
            buffer.SetGlobalVectorArray(otherShadowTilesId, otherShadowTiles);

            buffer.SetKeywords(otherFilterKeywords, (int)settings.other.filter - 1);

            buffer.EndSample(bufferName);
            ExecuteBuffer();
        }
        private void RenderSpotShadows(int index, int split, int tileSize)
        {
            ShadowedOtherLight light = shadowedOtherLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);
            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
            shadowSettings.splitData = splitData;

            float texelSize = 2f / (tileSize * projectionMatrix.m00);
            float filterSize = texelSize * ((float)settings.other.filter + 1f);
            float bias = light.normalBias * filterSize * 1.4142136f;
            Vector2 offset = SetTileViewport(index, split, tileSize);
            float tileScale = 1f / split;
            SetOtherTileData(index, offset, tileScale, bias);
            otherShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);
            buffer.SetGlobalDepthBias(0f, 0f);
        }
        private void RenderPointShadows(int index, int split, int tileSize)
        {
            ShadowedOtherLight light = shadowedOtherLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

            float texelSize = 2f / tileSize;
            float filterSize = texelSize * ((float)settings.other.filter + 1f);
            float bias = light.normalBias * filterSize * 1.4142136f;
            float tileScale = 1f / split;

            float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;

            for (int i = 0; i < 6; i++)
            {
                cullingResults.ComputePointShadowMatricesAndCullingPrimitives(light.visibleLightIndex, (CubemapFace)i, fovBias, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
                viewMatrix.m11 = -viewMatrix.m11;
                viewMatrix.m12 = -viewMatrix.m12;
                viewMatrix.m13 = -viewMatrix.m13;
                shadowSettings.splitData = splitData;

                int tileIndex = index + i;
                Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
                SetOtherTileData(tileIndex, offset, tileScale, bias);
                otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

                buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
                ExecuteBuffer();
                context.DrawShadows(ref shadowSettings);
                buffer.SetGlobalDepthBias(0f, 0f);
            }
        }
        private void SetOtherTileData(int index, Vector2 offset, float scale, float bias)
        {
            float border = atlasSizes.w * 0.5f;
            Vector4 data;
            data.x = offset.x * scale + border;
            data.y = offset.y * scale + border;
            data.z = scale - border - border;
            data.w = bias;
            otherShadowTiles[index] = data;
        }

        private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, float scale)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            return m;
        }
        private Vector2 SetTileViewport(int index, int split, float tileSize)
        {
            Vector2 offset = new Vector2(index % split, index / split);
            buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        private void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
    }
}
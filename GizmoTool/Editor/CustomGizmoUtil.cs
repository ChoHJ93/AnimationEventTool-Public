using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using Object = UnityEngine.Object;

namespace GizmoTool
{
    public class CustomGizmoUtil
    {
        public enum Style
        {
            Wireframe,
            SolidColor,
            FlatShaded,
            SmoothShaded,
        };

        private static bool s_isGizmoVisible => EditorPrefs.GetBool(CustomGizmoSetting.SHOW_GIZMO_KEY, false);
        private static float s_wireframeZBias = 1.0e-4f;

        private const int kNormalFlag = 1 << 0;
        private const int kCapShiftScaleFlag = 1 << 1;
        private const int kDepthTestFlag = 1 << 2;

        private static Dictionary<int, Material> s_materialPool;
        private static MaterialPropertyBlock s_materialProperties;
        private static MaterialPropertyBlock s_extraMaterialProperties; //for Cone Mesh

        private static MaterialPropertyBlock GetMaterialPropertyBlock()
        {
            return (s_materialProperties != null) ? s_materialProperties : (s_materialProperties = new MaterialPropertyBlock());
        }
        private static MaterialPropertyBlock GetExtraMaterialPropertyBlock()
        {
            return (s_extraMaterialProperties != null) ? s_extraMaterialProperties : (s_extraMaterialProperties = new MaterialPropertyBlock());
        }

        private static Material GetMaterial(Style style, bool depthTest, bool capShiftScale)
        {
            int key = 0;

            switch (style)
            {
                case Style.FlatShaded:
                case Style.SmoothShaded:
                    key |= kNormalFlag;
                    break;
            }

            if (capShiftScale)
                key |= kCapShiftScaleFlag;

            if (depthTest)
                key |= kDepthTestFlag;

            if (s_materialPool == null)
                s_materialPool = new Dictionary<int, Material>();

            Material material;
            if (!s_materialPool.TryGetValue(key, out material) || material == null)
            {
                if (material == null)
                    s_materialPool.Remove(key);

                string shaderName = depthTest ? "CHJ_Custom/Primitive" : "CHJ_Custom/PrimitiveNoZTest";

                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                    return null;

                material = new Material(shader);

                if ((key & kNormalFlag) != 0)
                    material.EnableKeyword("NORMAL_ON");

                if ((key & kCapShiftScaleFlag) != 0)
                    material.EnableKeyword("CAP_SHIFT_SCALE");

                s_materialPool.Add(key, material);
            }

            //if (depthTest)
            //    material.renderQueue = ((int)RenderQueue.Geometry); Debug.Log($"fkn;as' {material.renderQueue}");

            return material;
        }

        private static Material GetHandleMaterial()
        {
            int key = "HandleShader".GetHashCode();

            if (s_materialPool == null)
                s_materialPool = new Dictionary<int, Material>();

            Material material;
            if (!s_materialPool.TryGetValue(key, out material) || material == null)
            {
                if (material == null)
                    s_materialPool.Remove(key);

                string shaderName = "CHJ_Custom/HandleShader";

                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                    return null;

                material = new Material(shader);

                s_materialPool.Add(key, material);
            }

            //if (depthTest)
            //    material.renderQueue = ((int)RenderQueue.Geometry); Debug.Log($"fkn;as' {material.renderQueue}");

            return material;
        }

        #region Get GizmoDrawInfo_Internal
        private static GizmoDrawInfo[] GetArrowDrawInfo_Internal(Vector3 from, Vector3 to, float coneRadius, float coneHeight, float stemThickness, int numSegments, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            Vector3 axisY = to - from;
            float axisLength = axisY.magnitude;
            if (axisLength < CustomEditorUtil.EPSILON)
                return null;

            axisY.Normalize();

            Vector3 axisYCrosser = Mathf.Abs(Vector3.Dot(axisY, Vector3.up)) < 0.5f ? Vector3.up : Vector3.forward;
            Vector3 tangent = Vector3.Normalize(Vector3.Cross(axisYCrosser, axisY));
            Quaternion rotation = Quaternion.LookRotation(tangent, axisY);

            Vector3 coneBaseCenter = to - coneHeight * axisY; // top of cone ends at "to"

            List<GizmoDrawInfo> drawInfos = new List<GizmoDrawInfo>();
            GizmoDrawInfo drawInfo_Head = GetConeDrawInfo_Internal(coneBaseCenter, rotation, coneHeight, coneRadius, numSegments, color, style, depthTest, newMatProp);
            GizmoDrawInfo drawInfo_Body = null;
            if (stemThickness <= 0.0f)
            {
                if (style == Style.Wireframe)
                    to -= coneHeight * axisY;

                drawInfo_Body = GetLineDrawInfo_Internal(from, to, color, depthTest, newMatProp);
            }
            else if (coneHeight < axisLength)
            {
                to -= coneHeight * axisY;

                drawInfo_Body = GetCylinderDrawInfo_Internal(from, to, 0.5f * stemThickness, numSegments, color, style, depthTest, newMatProp);
            }

            drawInfo_Head.CustomBounds = new Bounds();
            drawInfos.Add(drawInfo_Head);

            if (drawInfo_Body != null)
            {
                Vector3 boundSize = drawInfo_Body.Mesh.bounds.size;
                boundSize.x *= 0.5f * stemThickness;
                boundSize.z *= 0.5f * stemThickness;
                Vector3 boundPos = drawInfo_Body.Position;
                drawInfo_Body.CustomBounds = new Bounds(boundPos, boundSize);
                drawInfos.Add(drawInfo_Body);
            }

            return drawInfos.ToArray();
        }
        private static GizmoDrawInfo[] GetArrowDrawInfo_Internal(Vector3 from, Vector3 direction, float length, float size, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            direction = direction.normalized;
            Vector3 to = from + direction * length;
            float coneRadius = size * 0.15f;
            float coneHeight = size * 0.4f;
            int numSegments = 14;
            float stemThickness = size * 0.1f;

            return GetArrowDrawInfo_Internal(from, to, coneRadius, coneHeight, stemThickness, numSegments, color, style, depthTest, newMatProp);
        }
        private static GizmoDrawInfo GetConeDrawInfo_Internal(Vector3 baseCenter, Quaternion rotation, float height, float radius, int numSegments, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            if (height < CustomEditorUtil.EPSILON || radius < CustomEditorUtil.EPSILON)
                return null;

            Mesh mesh = null;
            switch (style)
            {
                case Style.Wireframe:
                    mesh = PrimitiveMeshFactory.ConeWireframe(numSegments);
                    break;
                case Style.SolidColor:
                    mesh = PrimitiveMeshFactory.ConeSolidColor(numSegments);
                    break;
                case Style.FlatShaded:
                    mesh = PrimitiveMeshFactory.ConeFlatShaded(numSegments);
                    break;
                case Style.SmoothShaded:
                    mesh = PrimitiveMeshFactory.ConeSmoothShaded(numSegments);
                    break;
            }
            if (mesh == null)
                return null;

            mesh.bounds = new Bounds(mesh.bounds.center, new Vector3(radius, height, radius));

            Material material = GetMaterial(style, depthTest, false);
            MaterialPropertyBlock materialProperties = newMatProp ? new MaterialPropertyBlock() : GetExtraMaterialPropertyBlock();
            Vector4 demensions = new Vector4(radius, height, radius, 0.0f);
            materialProperties.SetColor("_Color", color);
            materialProperties.SetVector("_Dimensions", demensions);
            materialProperties.SetFloat("_ZWrite", depthTest ? 1f : 0f);
            materialProperties.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0.0f);

            material.SetPass(0);

            GizmoDrawInfo drawInfo = new GizmoDrawInfo(mesh, material, materialProperties, baseCenter, rotation, demensions);
            return drawInfo;
        }
        private static GizmoDrawInfo GetConeDrawInfo_Internal(Vector3 from, Vector3 to, float radius, int numSegments, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            Vector3 axisY = to - from;
            float height = axisY.magnitude;
            if (height < CustomEditorUtil.EPSILON)
                return null;

            axisY.Normalize();

            Vector3 axisYCrosser = Mathf.Abs(Vector3.Dot(axisY.normalized, Vector3.up)) < 0.5f ? Vector3.up : Vector3.forward;
            Vector3 tangent = Vector3.Normalize(Vector3.Cross(axisYCrosser, axisY));
            Quaternion rotation = Quaternion.LookRotation(tangent, axisY);
            return GetConeDrawInfo_Internal(from, rotation, height, radius, numSegments, color, style, depthTest, newMatProp);
        }
        private static GizmoDrawInfo GetLineDrawInfo_Internal(Vector3 v0, Vector3 v1, Color color, bool depthTest, bool newMatProp = false)
        {
            Mesh mesh = PrimitiveMeshFactory.Line(v0, v1);
            if (mesh == null)
                return null;

            Material material = GetMaterial(Style.Wireframe, depthTest, false);
            MaterialPropertyBlock materialProperties = newMatProp ? new MaterialPropertyBlock() : GetMaterialPropertyBlock();

            Vector4 demensions = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);
            materialProperties.SetColor("_Color", color);
            materialProperties.SetVector("_Dimensions", demensions);
            materialProperties.SetFloat("_ZWrite", depthTest ? 1f : 0f);
            materialProperties.SetFloat("_ZBias", s_wireframeZBias);

            material.SetPass(0);

            GizmoDrawInfo drawInfo = new GizmoDrawInfo(mesh, material, materialProperties, Vector3.zero, Quaternion.identity, demensions);
            return drawInfo;
        }
        private static GizmoDrawInfo GetSphereDrawInfo_Internal(Vector3 center, float radius, int numSegments, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            if (radius < CustomEditorUtil.EPSILON)
                return null;

            Mesh mesh = null;
            switch (style)
            {
                case Style.Wireframe:
                    mesh = PrimitiveMeshFactory.SphereWireframe(numSegments, numSegments * 2);
                    break;
                case Style.SolidColor:
                    mesh = PrimitiveMeshFactory.SphereSolidColor(numSegments, numSegments * 2);
                    break;
                case Style.FlatShaded:
                    mesh = PrimitiveMeshFactory.SphereFlatShaded(numSegments, numSegments * 2);
                    break;
                case Style.SmoothShaded:
                    mesh = PrimitiveMeshFactory.SphereSmoothShaded(numSegments, numSegments * 2);
                    break;
            }
            if (mesh == null)
                return null;


            Material material = GetMaterial(style, depthTest, false);
            MaterialPropertyBlock materialProperties = newMatProp ? new MaterialPropertyBlock() : GetExtraMaterialPropertyBlock();
            Vector4 demensions = new Vector4(radius, radius, radius, 0.0f);
            materialProperties.SetColor("_Color", color);
            materialProperties.SetVector("_Dimensions", demensions);
            materialProperties.SetFloat("_ZWrite", depthTest ? 1f : 0f);
            materialProperties.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0.0f);

            material.SetPass(0);
            GizmoDrawInfo drawInfo = new GizmoDrawInfo(mesh, material, materialProperties, center, Quaternion.identity, demensions);
            return drawInfo;
        }
        private static GizmoDrawInfo GetCylinderDrawInfo_Internal(Vector3 point0, Vector3 point1, float radius, int numSegments, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            Vector3 axisY = point1 - point0;
            float height = axisY.magnitude;
            if (height < CustomEditorUtil.EPSILON)
                return null;

            axisY.Normalize();

            Vector3 center = 0.5f * (point0 + point1);

            Vector3 axisYCrosser = Mathf.Abs(Vector3.Dot(axisY.normalized, Vector3.up)) < 0.5f ? Vector3.up : Vector3.forward;
            Vector3 tangent = Vector3.Normalize(Vector3.Cross(axisYCrosser, axisY));
            Quaternion rotation = Quaternion.LookRotation(tangent, axisY);
            return GetCylinderDrawInfo_Internal(center, rotation, height, radius, numSegments, color, style, depthTest, newMatProp);
        }
        private static GizmoDrawInfo GetCylinderDrawInfo_Internal(Vector3 center, Quaternion rotation, float height, float radius, int numSegments, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            if (height < CustomEditorUtil.EPSILON || radius < CustomEditorUtil.EPSILON)
                return null;

            Mesh mesh = null;
            switch (style)
            {
                case Style.Wireframe:
                    mesh = PrimitiveMeshFactory.CylinderWireframe(numSegments);
                    break;
                case Style.SolidColor:
                    mesh = PrimitiveMeshFactory.CylinderSolidColor(numSegments);
                    break;
                case Style.FlatShaded:
                    mesh = PrimitiveMeshFactory.CylinderFlatShaded(numSegments);
                    break;
                case Style.SmoothShaded:
                    mesh = PrimitiveMeshFactory.CylinderSmoothShaded(numSegments);
                    break;
            }
            if (mesh == null)
                return null;

            mesh.bounds = new Bounds(mesh.bounds.center, new Vector3(radius, height, radius));
            Material material = GetMaterial(style, depthTest, true);
            MaterialPropertyBlock materialProperties = newMatProp ? new MaterialPropertyBlock() : GetMaterialPropertyBlock();
            Vector4 demensions = new Vector4(radius, radius, radius, height);
            materialProperties.SetColor("_Color", color);
            materialProperties.SetVector("_Dimensions", demensions);
            materialProperties.SetFloat("_ZWrite", depthTest ? 1f : 0f);
            materialProperties.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0.0f);


            material.SetPass(0);

            GizmoDrawInfo drawInfo = new GizmoDrawInfo(mesh, material, materialProperties, center, rotation, demensions);
            return drawInfo;
        }
        private static GizmoDrawInfo GetCapsuleDrawInfo_Internal(Vector3 center, Quaternion rotation, float height, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            if (height < CustomEditorUtil.EPSILON || radius < CustomEditorUtil.EPSILON)
                return null;

            Mesh meshCaps = null;
            Mesh meshSides = null;
            switch (style)
            {
                case Style.Wireframe:
                    meshCaps = PrimitiveMeshFactory.CapsuleWireframe(latSegmentsPerCap, longSegmentsPerCap, true, true, false);
                    meshSides = PrimitiveMeshFactory.CapsuleWireframe(latSegmentsPerCap, longSegmentsPerCap, false, false, true);
                    break;
                case Style.SolidColor:
                    meshCaps = PrimitiveMeshFactory.CapsuleSolidColor(latSegmentsPerCap, longSegmentsPerCap, true, true, false);
                    meshSides = PrimitiveMeshFactory.CapsuleSolidColor(latSegmentsPerCap, longSegmentsPerCap, false, false, true);
                    break;
                case Style.FlatShaded:
                    meshCaps = PrimitiveMeshFactory.CapsuleFlatShaded(latSegmentsPerCap, longSegmentsPerCap, true, true, false);
                    meshSides = PrimitiveMeshFactory.CapsuleFlatShaded(latSegmentsPerCap, longSegmentsPerCap, false, false, true);
                    break;
                case Style.SmoothShaded:
                    meshCaps = PrimitiveMeshFactory.CapsuleSmoothShaded(latSegmentsPerCap, longSegmentsPerCap, true, true, false);
                    meshSides = PrimitiveMeshFactory.CapsuleSmoothShaded(latSegmentsPerCap, longSegmentsPerCap, false, false, true);
                    break;
            }
            if (meshCaps == null || meshSides == null)
                return null;

            Vector3 axisY = rotation * Vector3.up;
            Vector3 topCapOffset = 0.5f * (height - radius) * axisY;
            Vector3 topCapCenter = center + topCapOffset;
            Vector3 bottomCapCenter = center - topCapOffset;

            Quaternion bottomCapRotation = Quaternion.AngleAxis(180.0f, axisY) * rotation;

            Material material = GetMaterial(style, depthTest, true);
            MaterialPropertyBlock materialProperties = newMatProp ? new MaterialPropertyBlock() : GetMaterialPropertyBlock();
            Vector4 demensions = new Vector4(radius, radius, radius, height);

            materialProperties.SetColor("_Color", color);
            materialProperties.SetVector("_Dimensions", demensions);
            materialProperties.SetFloat("_ZWrite", depthTest ? 1f : 0f);
            materialProperties.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0.0f);

            material.SetPass(0);

            List<GizmoDrawInfo> drawInfos = new List<GizmoDrawInfo>();
            GizmoDrawInfo drawInfo_Top = new GizmoDrawInfo(meshCaps, material, materialProperties, center, rotation, demensions);

            return drawInfo_Top;
        }
        private static GizmoDrawInfo GetCapsuleDrawInfo_Internal(Vector3 point0, Vector3 point1, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            Vector3 axisY = point1 - point0;
            float height = axisY.magnitude;
            if (height < CustomEditorUtil.EPSILON)
                return null;

            axisY.Normalize();

            Vector3 center = 0.5f * (point0 + point1);
            Vector3 axisYCrosser = Mathf.Abs(Vector3.Dot(axisY.normalized, Vector3.up)) < 0.5f ? Vector3.up : Vector3.forward;
            Vector3 tangent = Vector3.Normalize(Vector3.Cross(axisYCrosser, axisY));
            Quaternion rotation = Quaternion.LookRotation(tangent, axisY);

            return GetCapsuleDrawInfo_Internal(center, rotation, height, radius, latSegmentsPerCap, longSegmentsPerCap, color, style, depthTest, newMatProp);
        }
        private static GizmoDrawInfo GetBoxDrawInfo_Internal(Vector3 center, Quaternion rotation, Vector3 size, Color color, Style style, bool depthTest, bool newMatProp = false)
        {
            if (size.x < CustomEditorUtil.EPSILON || size.y < CustomEditorUtil.EPSILON || size.z < CustomEditorUtil.EPSILON)
                return null;
            Mesh mesh = null;
            switch (style)
            {
                case Style.Wireframe:
                    mesh = PrimitiveMeshFactory.BoxWireframe(size);
                    break;
                case Style.SolidColor:
                case Style.FlatShaded:
                case Style.SmoothShaded:
                    mesh = PrimitiveMeshFactory.BoxFlatShaded(size);
                    break;
            }
            if (mesh == null)
                return null;

            Material material = GetMaterial(Style.Wireframe, depthTest, false);
            MaterialPropertyBlock materialProperties = newMatProp ? new MaterialPropertyBlock() : GetMaterialPropertyBlock();
            Vector4 demensions = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);
            materialProperties.SetColor("_Color", color);
            materialProperties.SetVector("_Dimensions", demensions);
            materialProperties.SetFloat("_ZWrite", depthTest ? 1f : 0f);
            materialProperties.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0.0f);

            material.SetPass(0);

            GizmoDrawInfo drawInfo = new GizmoDrawInfo(mesh, material, materialProperties, center, rotation, demensions);
            return drawInfo;
        }
        private static GizmoDrawInfo GetCircleDrawInfo_Internal(Vector3 center, Color color, float radius, int numSegments, Style style, bool depthTest, bool newMatProp = false)
        {
            if (radius < CustomEditorUtil.EPSILON)
                return null;

            Mesh mesh = CreateCircle(radius, numSegments);
            switch (style)
            {
                case Style.Wireframe:
                    mesh = PrimitiveMeshFactory.CylinderWireframe(numSegments);
                    break;
                case Style.SolidColor:
                    mesh = PrimitiveMeshFactory.CylinderSolidColor(numSegments);
                    break;
                case Style.FlatShaded:
                    mesh = PrimitiveMeshFactory.CylinderFlatShaded(numSegments);
                    break;
                case Style.SmoothShaded:
                    mesh = PrimitiveMeshFactory.CylinderSmoothShaded(numSegments);
                    break;
            }
            if (mesh == null)
                return null;


            Material material = GetMaterial(style, depthTest, true);
            MaterialPropertyBlock materialProperties = newMatProp ? new MaterialPropertyBlock() : GetMaterialPropertyBlock();
            Vector4 demensions = new Vector4(radius, radius, radius, 0);
            materialProperties.SetColor("_Color", color);
            materialProperties.SetVector("_Dimensions", demensions);
            materialProperties.SetFloat("_ZWrite", depthTest ? 1f : 0f);
            materialProperties.SetFloat("_ZBias", (style == Style.Wireframe) ? s_wireframeZBias : 0.0f);

            Quaternion rotation = Quaternion.LookRotation((Camera.main.transform.position - center).normalized);
            rotation *= Quaternion.Euler(90, 0, 0);
            material.SetPass(0);

            GizmoDrawInfo drawInfo = new GizmoDrawInfo(mesh, material, materialProperties, center, rotation, demensions);
            return drawInfo;
        }
        private static GizmoDrawInfo GetTorusDrawInfo_Internal(Vector3 center, float radius, float thickness, int sideCount, int numSegments, Color color, bool depthTest, bool newMatProp = false)
        {
            if (radius < CustomEditorUtil.EPSILON)
                return null;
            Mesh mesh = PrimitiveMeshFactory.CreateTorus(radius, thickness, numSegments, sideCount);

            Material material = GetHandleMaterial();
            MaterialPropertyBlock materialProperties = newMatProp ? new MaterialPropertyBlock() : GetMaterialPropertyBlock();

            material.SetVector("_CameraPosition", Camera.main.transform.position);
            material.SetFloat("_CameraDistance", Vector3.Distance(Camera.main.transform.position, center));
            materialProperties.SetColor("_Color", color);
            materialProperties.SetFloat("_ZWrite", depthTest ? 1f : 0f);

            Quaternion rotation = Quaternion.LookRotation((Camera.main.transform.position - center).normalized);
            rotation *= Quaternion.Euler(90, 0, 0);
            material.SetPass(0);

            GizmoDrawInfo drawInfo = new GizmoDrawInfo(mesh, material, materialProperties, center, rotation, Vector4.one);
            return drawInfo;
        }
        #endregion

        #region Get GizmoDrawInfo
        public static GizmoDrawInfo[] GetArrowDrawInfo(Vector3 from, Vector3 to, float coneRadius, float coneHeight, float stemThickness, int numSegments, Color color, Style style, bool depthTest)
        {
            return GetArrowDrawInfo_Internal(from, to, coneRadius, coneHeight, stemThickness, numSegments, color, style, depthTest, true);
        }
        public static GizmoDrawInfo[] GetArrowDrawInfo(Vector3 from, Vector3 direction, float length, float size, Color color, Style style, bool depthTest)
        {
            return GetArrowDrawInfo_Internal(from, direction, length, size, color, style, depthTest, true);
        }
        public static GizmoDrawInfo GetConeDrawInfo(Vector3 baseCenter, Quaternion rotation, float height, float radius, int numSegments, Color color, Style style, bool depthTest)
        {
            return GetConeDrawInfo_Internal(baseCenter, rotation, height, radius, numSegments, color, style, depthTest, true);
        }
        public static GizmoDrawInfo GetLineDrawInfo(Vector3 v0, Vector3 v1, Color color, bool depthTest)
        {
            return GetLineDrawInfo_Internal(v0, v1, color, depthTest, true);
        }
        public static GizmoDrawInfo GetSphereDrawInfo(Vector3 center, float radius, int numSegments, Color color, Style style, bool depthTest)
        {
            return GetSphereDrawInfo_Internal(center, radius, numSegments, color, style, depthTest, true);
        }
        public static GizmoDrawInfo GetCylinderDrawInfo(Vector3 point0, Vector3 point1, float radius, int numSegments, Color color, Style style, bool depthTest)
        {
            return GetCylinderDrawInfo_Internal(point0, point1, radius, numSegments, color, style, depthTest, true);
        }
        public static GizmoDrawInfo GetCylinderDrawInfo(Vector3 center, Quaternion rotation, float height, float radius, int numSegments, Color color, Style style, bool depthTest)
        {
            return GetCylinderDrawInfo_Internal(center, rotation, height, radius, numSegments, color, style, depthTest, true);
        }
        public static GizmoDrawInfo GetCapsuleDrawInfo(Vector3 center, Quaternion rotation, float height, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, Style style, bool depthTest)
        {
            return GetCapsuleDrawInfo_Internal(center, rotation, height, radius, latSegmentsPerCap, longSegmentsPerCap, color, style, depthTest, true);
        }
        public static GizmoDrawInfo GetCapsuleDrawInfo(Vector3 point0, Vector3 point1, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, Style style, bool depthTest)
        {
            return GetCapsuleDrawInfo_Internal(point0, point1, radius, latSegmentsPerCap, longSegmentsPerCap, color, style, depthTest, true);
        }
        public static GizmoDrawInfo GetBoxDrawInfo(Vector3 center, Quaternion rotation, Vector3 size, Color color, Style style, bool depthTest)
        {
            return GetBoxDrawInfo_Internal(center, rotation, size, color, style, depthTest, true);
        }
        public static GizmoDrawInfo GetCircleDrawInfo(Vector3 center, Color color, float radius, int numSegments, Style style, bool depthTest)
        {
            return GetCircleDrawInfo_Internal(center, color, radius, numSegments, style, depthTest);
        }
        public static GizmoDrawInfo GetTorusDrawInfo(Vector3 center, float radius, float thickness, int sideCount, int numSegments, Color color, bool depthTest)
        {
            return GetTorusDrawInfo_Internal(center, radius, thickness, sideCount, numSegments, color, depthTest, true);
        }

        #endregion

        #region Draw-by-Graphics.DrawMesh
        public static void DrawBox(Vector3 center, Quaternion rotation, Vector3 size, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetBoxDrawInfo_Internal(center, rotation, size, color, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawArrow(Vector3 from, Vector3 to, float coneRadius, float coneHeight, int numSegments, float stemThickness, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo[] drawInfos = GetArrowDrawInfo_Internal(from, to, coneRadius, coneHeight, stemThickness, numSegments, color, style, depthTest);
            if (drawInfos == null || drawInfos.Length == 0 || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfos[0].Mesh, drawInfos[0].Position, drawInfos[0].Rotation, drawInfos[0].Material, layer, camera, 0, drawInfos[0].MaterialProperty, false, false, false);

            if (drawInfos.Length > 1)
                Graphics.DrawMesh(drawInfos[1].Mesh, drawInfos[1].Position, drawInfos[1].Rotation, drawInfos[1].Material, layer, camera, 0, drawInfos[1].MaterialProperty, false, false, false);
        }
        public static void DrawArrow(Vector3 from, Vector3 direction, float length, float size, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo[] drawInfos = GetArrowDrawInfo_Internal(from, direction, length, size, color, style, depthTest);
            if (drawInfos == null || drawInfos.Length == 0 || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfos[0].Mesh, drawInfos[0].Position, drawInfos[0].Rotation, drawInfos[0].Material, layer, camera, 0, drawInfos[0].MaterialProperty, false, false, false);

            if (drawInfos.Length > 1)
                Graphics.DrawMesh(drawInfos[1].Mesh, drawInfos[1].Position, drawInfos[1].Rotation, drawInfos[1].Material, layer, camera, 0, drawInfos[1].MaterialProperty, false, false, false);

        }
        public static void DrawCone(Vector3 baseCenter, Quaternion rotation, float height, float radius, int numSegments, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetConeDrawInfo_Internal(baseCenter, rotation, height, radius, numSegments, color, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawCone(Vector3 from, Vector3 to, float radius, int numSegments, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetConeDrawInfo_Internal(from, to, radius, numSegments, color, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawLine(Vector3 v0, Vector3 v1, Color color, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetLineDrawInfo_Internal(v0, v1, color, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawSphere(Vector3 center, float radius, Color color, int numSegments = 180, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetSphereDrawInfo_Internal(center, radius, numSegments, color, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawCylinder(Vector3 center, Quaternion rotation, float height, float radius, int numSegments, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetCylinderDrawInfo_Internal(center, rotation, height, radius, numSegments, color, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawCylinder(Vector3 point0, Vector3 point1, float radius, int numSegments, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetCylinderDrawInfo_Internal(point0, point1, radius, numSegments, color, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawCapsule(Vector3 center, Quaternion rotation, float height, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetCapsuleDrawInfo_Internal(center, rotation, height, radius, latSegmentsPerCap, longSegmentsPerCap, color, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawCapsule(Vector3 point0, Vector3 point1, float radius, int latSegmentsPerCap, int longSegmentsPerCap, Color color, Style style = Style.Wireframe, bool depthTest = true, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetCapsuleDrawInfo_Internal(point0, point1, radius, latSegmentsPerCap, longSegmentsPerCap, color, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        public static void DrawCircle(Vector3 center, Color color, float radius = 0.5f, int numSegments = 360, bool depthTest = false, Style style = Style.Wireframe, int layer = 0, Camera camera = null)
        {
            GizmoDrawInfo drawInfo = GetCircleDrawInfo_Internal(center, color, radius, numSegments, style, depthTest);
            if (drawInfo == null || !s_isGizmoVisible) return;
            Graphics.DrawMesh(drawInfo.Mesh, drawInfo.Position, drawInfo.Rotation, drawInfo.Material, layer, camera, 0, drawInfo.MaterialProperty, false, false, false);
        }
        #endregion

        public static MeshRenderer[] AddGizmoMeshRenderer(Transform parentTr, GizmoDrawInfo[] drawInfo, string name)
        {
            if (parentTr == null || drawInfo == null || drawInfo.Length == 0)
                return null;


            Transform rootTr = new GameObject(name).transform;
            rootTr.SetParent(parentTr);
            rootTr.localPosition = Vector3.zero;
            rootTr.rotation = Quaternion.identity;
            MeshRenderer[] renderers = new MeshRenderer[drawInfo.Length];
            for (int i = 0; i < drawInfo.Length; i++)
            {
                renderers[i] = AddGizmoMeshRenderer(rootTr, drawInfo[i], $"{name}_{i:D2}");
            }

            return renderers;
        }
        public static MeshRenderer AddGizmoMeshRenderer(Transform parentTr, GizmoDrawInfo drawInfo, string name)
        {
            if (parentTr == null || drawInfo == null)
                return null;

            MeshFilter mf0 = new GameObject(name).AddComponent<MeshFilter>();
            mf0.sharedMesh = drawInfo.Mesh;
            mf0.transform.position = drawInfo.Position;
            mf0.transform.rotation = drawInfo.Rotation;
            mf0.transform.localScale = drawInfo.Scale;
            mf0.transform.SetParent(parentTr);


            MeshRenderer mr0 = mf0.gameObject.AddComponent<MeshRenderer>();
            mr0.material = drawInfo.Material;
            drawInfo.MaterialProperty.SetVector("_Dimensions", Vector4.one);
            mr0.SetPropertyBlock(drawInfo.MaterialProperty);
            mr0.bounds = drawInfo.CustomBounds;
            //mr0.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            //mr0.receiveShadows = false;
            //mr0.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            //mr0.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return mr0;
        }

        private static Mesh CreateCircle(float radius = 0.5f, int numSegments = 360)
        {
            Vector3[] vertices = new Vector3[numSegments + 1];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] indices = new int[numSegments * 3];

            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            float angleStep = 360f / numSegments;

            for (int i = 1; i < vertices.Length; i++)
            {
                float angle = Mathf.Deg2Rad * angleStep * (i - 1);
                vertices[i] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                uv[i] = new Vector2(vertices[i].x / 2f + 0.5f, vertices[i].z / 2f + 0.5f);
            }

            for (int i = 1; i < vertices.Length; i++)
            {
                int index1 = 0;
                int index2 = i;
                int index3 = i + 1;

                if (index3 == vertices.Length)
                    index3 = 1;

                indices[(i - 1) * 3 + 0] = index1;
                indices[(i - 1) * 3 + 1] = index2;
                indices[(i - 1) * 3 + 2] = index3;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = indices;

            return mesh;
        }

        public static List<Vector3> GetFanShapePoints(Vector3 startingPoint, Vector3 baseDir, float angle, float radius, int count)
        {
            List<Vector3> points = new List<Vector3>();

            // 부채꼴의 양 끝점 계산
            Vector3 directionRight = Quaternion.Euler(0, angle * 0.5f, 0) * baseDir;
            Vector3 directionLeft = Quaternion.Euler(0, -angle * 0.5f, 0) * baseDir;

            Vector3 pointRight = directionRight * radius + startingPoint;
            Vector3 pointLeft = directionLeft * radius + startingPoint;

            // 양 끝점 추가
            points.Add(pointRight);
            points.Add(pointLeft);

            // 중간 점들 계산
            float segmentAngle = angle / (count + 1);
            for (int i = 0; i <= count + 1; i++)
            {
                float currentAngle = -angle * 0.5f + segmentAngle * i;
                Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * baseDir;
                Vector3 point = direction * radius + startingPoint;
                points.Add(point);
            }

            return points;
        }
    }

    [System.Serializable]
    public class GizmoDrawInfo
    {
        public Mesh Mesh { get; private set; }
        public Material Material { get; private set; }
        private MaterialPropertyBlock materialProperty;
        public MaterialPropertyBlock MaterialProperty { get { return materialProperty ?? new MaterialPropertyBlock(); } private set { materialProperty = value; } }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Scale { get; private set; }
        /// <summary>
        /// optional
        /// </summary>
        public Bounds CustomBounds { get; set; }
        public GizmoDrawInfo(Mesh mesh, Material mat, MaterialPropertyBlock matProp, Vector3 pos, Quaternion rot, Vector4 dimensions)
        {
            Mesh = mesh;
            Material = mat;
            MaterialProperty = matProp;
            Position = pos;
            Rotation = rot;
            Scale = dimensions.w > 0 ? new Vector3(dimensions.x, dimensions.w, dimensions.z) : dimensions;
            CustomBounds = mesh.bounds;
        }
    }
}

#endif // UNITY_EDITOR
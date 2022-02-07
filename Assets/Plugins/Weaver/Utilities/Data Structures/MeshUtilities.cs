// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System.Collections.Generic;
using UnityEngine;

namespace Weaver
{
    /// <summary>Various utility methods for using the <see cref="MeshBuilder"/> class.</summary>
    public static class MeshUtilities
    {
        /************************************************************************************************************************/
        #region Mass Modification
        /************************************************************************************************************************/

        /// <summary>
        /// Adds the specified translation to all vertices in the specified range.
        /// </summary>
        public static void TranslateVertices(MeshBuilder builder, Vector3 translation, int start, int end)
        {
            while (start < end)
            {
                builder.Vertices[start++] += translation;
            }
        }

        /// <summary>
        /// Adds the specified translation to all vertices from the specified start index up to the current vertex count.
        /// </summary>
        public static void TranslateVertices(MeshBuilder builder, Vector3 translation, int start)
        {
            TranslateVertices(builder, translation, start, builder.VertexCount);
        }

        /// <summary>
        /// Adds the specified translation to all vertices up to the current vertex count.
        /// </summary>
        public static void TranslateVertices(MeshBuilder builder, Vector3 translation)
        {
            TranslateVertices(builder, translation, 0, builder.VertexCount);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Multiplies all vertices and normaly by `rotation`.
        /// </summary>
        public static void RotateVertices(MeshBuilder builder, Quaternion rotation)
        {
            for (int i = 0; i < builder.VertexCount; i++)
            {
                builder.Vertices[i] = rotation * builder.Vertices[i];
                builder.Normals[i] = rotation * builder.Normals[i];
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Multiplies all vertices by `scale`.
        /// </summary>
        public static void ScaleVertices(MeshBuilder builder, float scale)
        {
            for (int i = 0; i < builder.VertexCount; i++)
            {
                builder.Vertices[i] *= scale;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Duplication
        /************************************************************************************************************************/

        /// <summary>
        /// Adds a new vertex with its position at 'builder.Vertices[vertex] + offset' and the specified `normal` and `uv` values.
        /// </summary>
        public static void DuplicateVertex(MeshBuilder builder, int vertex, Vector3 offset, Vector3 normal, Vector2 uv)
        {
            builder.Vertices.Add(builder.Vertices[vertex] + offset);
            builder.Normals.Add(normal);
            builder.UVs.Add(uv);
        }

        /// <summary>
        /// Adds a new vertex with its position at `builder.Vertices[vertex]` and the specified `normal` and `uv` values.
        /// </summary>
        public static void DuplicateVertex(MeshBuilder builder, int vertex, Vector3 normal, Vector2 uv)
        {
            builder.Vertices.Add(builder.Vertices[vertex]);
            builder.Normals.Add(normal);
            builder.UVs.Add(uv);
        }

        /// <summary>
        /// Adds a new vertex with its position at `builder.Vertices[vertex]` and the specified `normal` value.
        /// </summary>
        public static void DuplicateVertex(MeshBuilder builder, int vertex, Vector3 normal)
        {
            builder.Vertices.Add(builder.Vertices[vertex]);
            builder.Normals.Add(normal);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Quads
        /************************************************************************************************************************/

        /// <summary>
        /// Adds 4 vertices to form a quad.
        /// </summary>
        public static void PlaceQuadVertices(List<Vector3> vertices, Vector3 bottomLeft, Vector2 size)
        {
            vertices.Add(bottomLeft);

            bottomLeft.y += size.y;
            vertices.Add(bottomLeft);

            bottomLeft.x += size.x;
            vertices.Add(bottomLeft);

            bottomLeft.y -= size.y;
            vertices.Add(bottomLeft);
        }

        /// <summary>
        /// Adds 4 UV values to planar map a quad.
        /// </summary>
        public static void PlaceQuadUVs(List<Vector2> uvs, Vector2 bottomLeft, Vector2 size)
        {
            uvs.Add(bottomLeft);

            bottomLeft.y += size.y;
            uvs.Add(bottomLeft);

            bottomLeft.x += size.x;
            uvs.Add(bottomLeft);

            bottomLeft.y -= size.y;
            uvs.Add(bottomLeft);
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Polygons
        /************************************************************************************************************************/

        /// <summary>
        /// Adds indices, vertices, normals, and UVs for a polygon on the XY plane with the specified parameters.
        /// </summary>
        public static void ShapeEquilateralPolygonXY(MeshBuilder builder, int subMesh, Vector3 center, int sides, float radius, Rect uvArea)
        {
            var indexCount = builder.Indices[subMesh].Count;
            for (int i = 0; i < sides - 2; i++)
            {
                builder.IndexTriangle(subMesh, indexCount, indexCount + i + 2, indexCount + i + 1);
            }

            var increment = Mathf.PI * 2 / sides;
            for (int i = 0; i < sides; i++)
            {
                var angle = i * increment;
                var offset = VectorXYFromAngle(angle, radius, 0);
                var u = offset.x.LinearRescale(-radius, radius, uvArea.xMin, uvArea.xMax);
                var v = offset.y.LinearRescale(-radius, radius, uvArea.yMin, uvArea.yMax);
                builder.Vertices.Add(center + offset);
                builder.Normals.Add(Vector3.back);
                builder.UVs.Add(new Vector2(u, v));
            }
        }

        /// <summary>
        /// Adds indices, vertices, normals, and UVs for a polygon on the XY plane with the specified parameters.
        /// </summary>
        public static void ShapeEquilateralPolygonXY(MeshBuilder builder, int subMesh, Vector3 center, int sides, float radius)
        {
            ShapeEquilateralPolygonXY(builder, subMesh, center, sides, radius, new Rect(0, 0, 1, 1));
        }

        /// <summary>
        /// Duplicates the vertex data of a polygon with the vertices offset by `extrusion` and adds indices to join
        /// them as faces to the original vertices.
        /// </summary>
        public static void ExtrudePolygon(MeshBuilder builder, int subMesh, int firstVertex, int sides, Vector3 extrusion)
        {
            var faceNormal = builder.Normals[firstVertex];

            var previousIndex = sides - 1;
            for (int i = 0; i < sides; i++)
            {
                var normal = Vector3.Cross(builder.Vertices[previousIndex] - builder.Vertices[i], faceNormal);

                builder.Index2Triangles(subMesh);
                DuplicateVertex(builder, previousIndex, normal, Vector2.zero);
                DuplicateVertex(builder, i, normal, Vector2.right);
                DuplicateVertex(builder, i, extrusion, normal, Vector2.one);
                DuplicateVertex(builder, previousIndex, extrusion, normal, Vector2.up);

                previousIndex = i;
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Rings
        /************************************************************************************************************************/

        /// <summary>
        /// Adds indices, vertices, and normals to form a ring on the XY plane with the specified parameters.
        /// </summary>
        public static void BuildRingXY(MeshBuilder builder, int sides, float innerRadius, float outerRadius)
        {
            var increment = Mathf.PI * 2 / sides;
            var halfIncrement = increment * 0.5f;

            for (int i = 0; i < sides; i++)
            {
                var angle = i * increment;
                builder.Vertices.Add(VectorXYFromAngle(angle - halfIncrement, innerRadius, 0));
                builder.Vertices.Add(VectorXYFromAngle(angle, outerRadius, 0));
                builder.Normals.Add(Vector3.back);
                builder.Normals.Add(Vector3.back);

                var i2 = i * 2;
                builder.IndexTriangle(0, i2, i2 + 2, i2 + 1);
                builder.IndexTriangle(0, i2 + 2, i2 + 3, i2 + 1);
            }

            var indexCount = builder.Indices[0].Count;

            var indices = builder.Indices[0];
            indices[indexCount - 5] -= sides * 2;
            indices[indexCount - 3] -= sides * 2;
            indices[indexCount - 2] -= sides * 2;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Planar maps the UV values for a range of vertices into the `uvArea` based on their XY positions relative to `vertexArea`.
        /// </summary>
        public static void PlanarMapXY(MeshBuilder builder, int start, int length, Rect vertexArea, Rect uvArea)
        {
            var scale = new Vector2(
                1 / (vertexArea.xMax - vertexArea.xMin) * (uvArea.xMax - uvArea.xMin),
                1 / (vertexArea.yMax - vertexArea.yMin) * (uvArea.yMax - uvArea.yMin));

            length += start;

            if (builder.UVs.Count < length)
                builder.UVs.SetCount(length);

            for (; start < length; start++)
            {
                var uv = builder.Vertices[start];
                uv.x = uvArea.xMin + (uv.x - vertexArea.xMin) * scale.x;
                uv.y = uvArea.yMin + (uv.y - vertexArea.yMin) * scale.y;
                builder.UVs[start] = uv;
            }
        }

        /// <summary>
        /// Planar maps the UV values for all vertices using their XY positions.
        /// </summary>
        public static void PlanarMapXY(MeshBuilder builder)
        {
            var vertexArea = new Rect(builder.Vertices[0].x, builder.Vertices[0].y, 0, 0);
            for (int i = 1; i < builder.VertexCount; i++)
            {
                var vertex = builder.Vertices[i];

                if (vertex.x < vertexArea.xMin) vertexArea.xMin = vertex.x;
                else if (vertex.x > vertexArea.xMax) vertexArea.xMax = vertex.x;

                if (vertex.y < vertexArea.yMin) vertexArea.yMin = vertex.y;
                else if (vertex.y > vertexArea.yMax) vertexArea.yMax = vertex.y;
            }

            PlanarMapXY(builder, 0, builder.VertexCount, vertexArea, new Rect(0, 0, 1, 1));
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Utils
        /************************************************************************************************************************/

        /// <summary>
        /// Adds members to the list (or removes them) until the count reaches the specified value.
        /// </summary>
        public static void SetCount<T>(this List<T> list, int count) where T : new()
        {
            if (count > list.Count)
            {
                do
                {
                    list.Add(new T());
                }
                while (count > list.Count);
            }
            else if (count < list.Count)
            {
                list.RemoveRange(count, list.Count - count);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Generates a vector with the specified z value and length in the specified direction on the XY axis.</summary>
        public static Vector3 VectorXYFromAngle(float radians, float length = 1, float z = 0)
        {
            return new Vector3(Mathf.Cos(radians) * length, Mathf.Sin(radians) * length, z);
        }

        /************************************************************************************************************************/

        /// <param name="builder">The <see cref="MeshBuilder"/> to get the UV channel from.</param>
        /// <param name="channel">The channel number (must be 1, 2, 3, or 4).</param>
        public static List<Vector4> GetUVs(MeshBuilder builder, int channel)
        {
            switch (channel)
            {
                case 1: return builder.UVs;
                case 2: return builder.UVs2;
                case 3: return builder.UVs3;
                case 4: return builder.UVs4;
                default:
                    throw new System.ArgumentException(
                        "MeshBuilderUtils.GetUvChannel only supports values of 1, 2, 3, and 4 since those are the only UV channels supported by the Mesh class");
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Doesn't take into account vertices that are shared by multiple triangles.
        /// Only the last triangle to include each vertex will determine its normal.
        /// </summary>
        public static void CalculateNormals(MeshBuilder builder, int subMesh, int minIndex, int maxIndex)
        {
            builder.Normals.Capacity = builder.Vertices.Capacity;

            var subMeshIndices = builder.Indices[subMesh];
            for (int i = 0; i < subMeshIndices.Count; i += 3)
            {
                var index0 = subMeshIndices[i];
                if (index0 >= minIndex && index0 < maxIndex)
                {
                    // Get the three indices that make the triangle.
                    var index1 = subMeshIndices[i + 1];
                    var index2 = subMeshIndices[i + 2];

                    // Get the three vertices that make the triangle.
                    var point1 = builder.Vertices[index0];
                    var point2 = builder.Vertices[index1];
                    var point3 = builder.Vertices[index2];

                    // Get the lines connecting those points.
                    var line1 = point2 - point1;
                    var line2 = point3 - point1;

                    // Use those lines to calculate the normal.
                    var normal = Vector3.Cross(line1, line2);
                    normal.Normalize();

                    // Assign the normal to all 3 vertices of the triangle.
                    builder.Normals[index0] = normal;
                    builder.Normals[index1] = normal;
                    builder.Normals[index2] = normal;
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}


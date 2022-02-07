// Weaver // https://kybernetik.com.au/weaver // Copyright 2021 Kybernetik //

using System.Collections.Generic;
using UnityEngine;

namespace Weaver
{
    /// <summary>
    /// Encapsulates lists of vertices, normals, etc. to simplify the procedural generation of meshes.
    /// <para></para>
    /// To use: simply add elements to the lists (<see cref="Vertices"/>, <see cref="Indices"/>, <see cref="UVs"/>,
    /// etc.) then call any of the Compile() overloads. You can also implicitly cast a <see cref="MeshBuilder"/> to a
    /// <see cref="Mesh"/>.
    /// <para></para>
    /// You can efficiently reuse a <see cref="MeshBuilder"/> by calling <see cref="Clear"/>.
    /// </summary>
    public class MeshBuilder
    {
        /************************************************************************************************************************/
        #region Fields and Properties
        /************************************************************************************************************************/

        /// <summary>The <see cref="MeshTopology"/> with which the <see cref="Mesh"/> will be compiled.</summary>
        public MeshTopology Topology { get; set; }

        /// <summary>The vertex positions which will be used for <see cref="Mesh.vertices"/>.</summary>
        public readonly List<Vector3> Vertices;

        /// <summary>The vertex normals which will be used for <see cref="Mesh.normals"/>.</summary>
        public readonly List<Vector3> Normals;

        /// <summary>The UV coordinates which will be used for <see cref="Mesh.uv"/>.</summary>
        public readonly List<Vector4> UVs;

        /// <summary>The UV coordinates which will be used for <see cref="Mesh.uv2"/>.</summary>
        public readonly List<Vector4> UVs2;

        /// <summary>The UV coordinates which will be used for <see cref="Mesh.uv3"/>.</summary>
        public readonly List<Vector4> UVs3;

        /// <summary>The UV coordinates which will be used for <see cref="Mesh.uv4"/>.</summary>
        public readonly List<Vector4> UVs4;

        /// <summary>The vertex tangents which will be used for <see cref="Mesh.tangents"/>.</summary>
        public readonly List<Vector4> Tangents;

        /// <summary>The vertex colors which will be used for <see cref="Mesh.colors"/>.</summary>
        public readonly List<Color> Colors;

        /// <summary>The mesh indices which will be used for <see cref="Mesh.triangles"/> (though the topology isn't necessarily triangles).</summary>
        public readonly List<int>[] Indices;

        /// <summary>The indices of the first sub-mesh.</summary>
        public List<int> Indices0 { get { return Indices[0]; } }

        /************************************************************************************************************************/

        /// <summary>The number of vertices which have currently been built.</summary>
        public int VertexCount
        {
            get { return Vertices.Count; }
            set
            {
                Vertices.SetCount(value);
                if (Normals != null) Normals.SetCount(value);
                if (UVs != null) UVs.SetCount(value);
                if (UVs2 != null) UVs2.SetCount(value);
                if (UVs3 != null) UVs3.SetCount(value);
                if (UVs4 != null) UVs4.SetCount(value);
                if (Tangents != null) Tangents.SetCount(value);
                if (Colors != null) Colors.SetCount(value);
            }
        }

        /// <summary>The number of indices which have currently been built in the first sub mesh.</summary>
        public int IndexCount0
        {
            get { return Indices[0].Count; }
            set { Indices[0].SetCount(value); }
        }

        /// <summary>The number of sub meshes.</summary>
        public int SubMeshCount { get { return Indices.Length; } }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Initialisation
        /************************************************************************************************************************/

        /// <summary>Constructs a mesh builder with UVs and Normals using the specified `topology`.</summary>
        public MeshBuilder(MeshTopology topology = MeshTopology.Triangles)
        {
            this.Topology = topology;

            Vertices = new List<Vector3>();
            Normals = new List<Vector3>();
            UVs = new List<Vector4>();

            Indices = new List<int>[] { new List<int>() };
        }

        /************************************************************************************************************************/

        /// <summary>Constructs a mesh builder with UVs and Normals using <see cref="MeshTopology.Triangles"/>.</summary>
        public MeshBuilder(int vertexCapacity, int indexCapacity)
        {
            Topology = MeshTopology.Triangles;

            Vertices = new List<Vector3>(vertexCapacity);
            Normals = new List<Vector3>(vertexCapacity);
            UVs = new List<Vector4>(vertexCapacity);

            Indices = new List<int>[] { new List<int>(indexCapacity) };
        }

        /************************************************************************************************************************/

        /// <summary>Constructs a mesh builder with UVs and Normals using <see cref="MeshTopology.Triangles"/>.</summary>
        public MeshBuilder(int vertexCapacity, params int[] indexCapacities)
            : this(MeshTopology.Triangles, vertexCapacity, indexCapacities)
        {
            Normals = new List<Vector3>(vertexCapacity);
            UVs = new List<Vector4>(vertexCapacity);
        }

        /************************************************************************************************************************/

        /// <summary>Constructs a mesh builder with a specific set of data channels and topology.</summary>
        public MeshBuilder(MeshChannel channels, MeshTopology topology = MeshTopology.Triangles)
        {
            this.Topology = topology;

            Vertices = new List<Vector3>();

            if ((channels & MeshChannel.Normals) != 0) Normals = new List<Vector3>();
            if ((channels & MeshChannel.UVs) != 0) UVs = new List<Vector4>();
            if ((channels & MeshChannel.UVs2) != 0) UVs2 = new List<Vector4>();
            if ((channels & MeshChannel.UVs3) != 0) UVs3 = new List<Vector4>();
            if ((channels & MeshChannel.UVs4) != 0) UVs4 = new List<Vector4>();
            if ((channels & MeshChannel.Tangents) != 0) Tangents = new List<Vector4>();
            if ((channels & MeshChannel.Colors) != 0) Colors = new List<Color>();

            Indices = new List<int>[] { new List<int>() };
        }

        /************************************************************************************************************************/

        /// <summary>Constructs a mesh builder with a specific set of data channels and topology.</summary>
        public MeshBuilder(MeshChannel channels, MeshTopology topology, int vertexCapacity, params int[] indexCapacities)
            : this(topology, vertexCapacity, indexCapacities)
        {
            if ((channels & MeshChannel.Normals) != 0) Normals = new List<Vector3>(vertexCapacity);
            if ((channels & MeshChannel.UVs) != 0) UVs = new List<Vector4>(vertexCapacity);
            if ((channels & MeshChannel.UVs2) != 0) UVs2 = new List<Vector4>(vertexCapacity);
            if ((channels & MeshChannel.UVs3) != 0) UVs3 = new List<Vector4>(vertexCapacity);
            if ((channels & MeshChannel.UVs4) != 0) UVs4 = new List<Vector4>(vertexCapacity);
            if ((channels & MeshChannel.Tangents) != 0) Tangents = new List<Vector4>(vertexCapacity);
            if ((channels & MeshChannel.Colors) != 0) Colors = new List<Color>(vertexCapacity);
        }

        /************************************************************************************************************************/

        private MeshBuilder(MeshTopology topology, int vertexCapacity, params int[] indexCapacities)
        {
#if UNITY_ASSERTIONS
            if (indexCapacities.IsNullOrEmpty())
                Debug.LogWarning("indexLimits must contain at least one element");
#endif

            Topology = topology;

            Vertices = new List<Vector3>(vertexCapacity);

            Indices = new List<int>[indexCapacities.Length];
            for (int i = 0; i < Indices.Length; i++)
            {
                Indices[i] = new List<int>(indexCapacities[i]);
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Flags for each of the optional channels a <see cref="Mesh"/> can contain.
        /// </summary>
        [System.Flags]
        public enum MeshChannel
        {
            /// <summary>First set of UV coordinates.</summary>
            UVs = 1 << 0,

            /// <summary>Second set of UV coordinates.</summary>
            UVs2 = 1 << 1,

            /// <summary>Third set of UV coordinates.</summary>
            UVs3 = 1 << 2,

            /// <summary>Fourth set of UV coordinates.</summary>
            UVs4 = 1 << 3,

            /// <summary>Vertex normals.</summary>
            Normals = 1 << 4,

            /// <summary>Vertex tangents.</summary>
            Tangents = 1 << 5,

            /// <summary>Vertex colors.</summary>
            Colors = 1 << 6,

            /// <summary>The default channels.</summary>
            Default = UVs | Normals,

            /// <summary>All optional channels.</summary>
            All = UVs | UVs2 | UVs3 | UVs4 | Normals | Tangents | Colors,
        }

        /************************************************************************************************************************/

        /// <summary>Clears this <see cref="MeshBuilder"/> to be ready for reuse.</summary>
        public virtual void Clear()
        {
            Vertices.Clear();
            if (Normals != null) Normals.Clear();
            if (UVs != null) UVs.Clear();
            if (UVs2 != null) UVs2.Clear();
            if (UVs3 != null) UVs3.Clear();
            if (UVs4 != null) UVs4.Clear();
            if (Tangents != null) Tangents.Clear();
            if (Colors != null) Colors.Clear();

            for (int i = 0; i < Indices.Length; i++)
                Indices[i].Clear();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Indices
        /************************************************************************************************************************/

        /// <summary>
        /// Adds indices for a triangle starting at the current vertex count: [+0][+1][+2].
        /// <para></para>You will generally want to call this method before adding the associated vertices.
        /// </summary>
        public void IndexTriangle(int subMesh = 0)
        {
            var indices = Indices[subMesh];

            indices.Add(VertexCount);
            indices.Add(VertexCount + 1);
            indices.Add(VertexCount + 2);
        }

        /// <summary>
        /// Adds indices for 2 triangles starting at the current vertex count: [+0][+1][+2] and [+0][+2][+3].
        /// <para></para>You will generally want to call this method before adding the associated vertices.
        /// </summary>
        public void Index2Triangles(int subMesh = 0)
        {
            var indices = Indices[subMesh];

            indices.Add(VertexCount);
            indices.Add(VertexCount + 1);
            indices.Add(VertexCount + 2);

            indices.Add(VertexCount);
            indices.Add(VertexCount + 2);
            indices.Add(VertexCount + 3);
        }

        /// <summary>
        /// Adds indices for 3 triangles in a fan starting at the current vertex count: [+0][+1][+2] and [+0][+2][+3] and [+0][+3][+4].
        /// <para></para>You will generally want to call this method before adding the associated vertices.
        /// </summary>
        public void Index3Triangles(int subMesh = 0)
        {
            var indices = Indices[subMesh];

            indices.Add(VertexCount);
            indices.Add(VertexCount + 1);
            indices.Add(VertexCount + 2);

            indices.Add(VertexCount);
            indices.Add(VertexCount + 2);
            indices.Add(VertexCount + 3);

            indices.Add(VertexCount);
            indices.Add(VertexCount + 3);
            indices.Add(VertexCount + 4);
        }

        /************************************************************************************************************************/

        /// <summary>Adds the specified indices to the specified sub mesh.</summary>
        public void IndexTriangle(int subMesh, int index0, int index1, int index2)
        {
            var indices = Indices[subMesh];

            indices.Add(index0);
            indices.Add(index1);
            indices.Add(index2);
        }

        /// <summary>Adds the specified indices to the specified sub mesh.</summary>
        public void Index2Triangles(int subMesh, int index0, int index1, int index2, int index3)
        {
            var indices = Indices[subMesh];

            indices.Add(index0);
            indices.Add(index1);
            indices.Add(index2);

            indices.Add(index0);
            indices.Add(index2);
            indices.Add(index3);
        }

        /// <summary>Adds the specified indices to the specified sub mesh.</summary>
        public void Index3Triangles(int subMesh, int index0, int index1, int index2, int index3, int index4)
        {
            var indices = Indices[subMesh];

            indices.Add(index0);
            indices.Add(index1);
            indices.Add(index2);

            indices.Add(index0);
            indices.Add(index2);
            indices.Add(index3);

            indices.Add(index0);
            indices.Add(index3);
            indices.Add(index4);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Adds a pair of indices for a line starting at the current vertex count: [+0][+1].
        /// <para></para>You will generally want to call this method before adding the associated vertices.
        /// </summary>
        public void IndexLine(int subMesh = 0)
        {
            var indices = Indices[subMesh];

            indices.Add(VertexCount);
            indices.Add(VertexCount + 1);
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Fills all the indices for the specified sub mesh such that 'Indices[subMesh][i] == i' up to the capacity
        /// of that sub mesh.
        /// </summary>
        public void FillIncrementalIndices(int subMesh = 0)
        {
            var indices = Indices[subMesh];
            indices.Capacity = Vertices.Count;

            while (indices.Count < indices.Capacity)
            {
                indices.Add(indices.Count);
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Compilation
        /************************************************************************************************************************/

        /// <summary>
        /// Assigns the data from this <see cref="MeshBuilder"/> to `mesh`.
        /// </summary>
        public virtual void Compile(Mesh mesh)
        {
            if (mesh.vertexCount > Vertices.Count) mesh.Clear();

            mesh.SetVertices(Vertices);
            if (Normals != null) mesh.SetNormals(Normals);
            if (UVs != null) mesh.SetUVs(0, UVs);
            if (UVs2 != null) mesh.SetUVs(1, UVs2);
            if (UVs3 != null) mesh.SetUVs(2, UVs3);
            if (UVs4 != null) mesh.SetUVs(3, UVs4);
            if (Tangents != null) mesh.SetTangents(Tangents);
            if (Colors != null) mesh.SetColors(Colors);

            mesh.subMeshCount = Indices.Length;

            // Triangles can be assigned directly from the list, but other topologies need to be converted to arrays :(
            if (Topology == MeshTopology.Triangles)
                for (int i = 0; i < mesh.subMeshCount; i++)
                    mesh.SetTriangles(Indices[i], i);
            else
                for (int i = 0; i < mesh.subMeshCount; i++)
                    mesh.SetIndices(Indices[i].ToArray(), Topology, i);
        }

        /// <summary>
        /// Creates a new <see cref="Mesh"/> if it is null and assigns the data from this <see cref="MeshBuilder"/> to it.
        /// </summary>
        public void Compile(ref Mesh mesh)
        {
            if (mesh == null)
                mesh = new Mesh();

            Compile(mesh);
        }

        /// <summary>
        /// Assigns the data from this <see cref="MeshBuilder"/> to a new <see cref="Mesh"/>.
        /// </summary>
        public Mesh Compile(string name = "Procedural Mesh")
        {
            var mesh = new Mesh { name = name };
            Compile(mesh);
            return mesh;
        }

        /// <summary>
        /// Assigns the data from this <see cref="MeshBuilder"/> to `meshFilter.mesh`.
        /// </summary>
        public void Compile(MeshFilter meshFilter)
        {
            if (meshFilter.sharedMesh != null)
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Compile(meshFilter.sharedMesh);
                    return;
                }
#endif

                Compile(meshFilter.mesh);
            }
            else
            {
#if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    meshFilter.sharedMesh = Compile();
                    return;
                }
#endif

                meshFilter.mesh = Compile();
            }
        }

        /// <summary>
        /// Assigns the data from this <see cref="MeshBuilder"/> to `meshCollider.mesh`.
        /// </summary>
        public void Compile(MeshCollider meshCollider)
        {
            if (VertexCount > 0)
            {
                if (meshCollider.sharedMesh != null)
                {
                    var mesh = meshCollider.sharedMesh;
                    Compile(mesh);

                    // Re-assign the MeshCollider to tell it that the mesh has changed.
                    meshCollider.sharedMesh = null;
                    meshCollider.sharedMesh = mesh;
                }
                else meshCollider.sharedMesh = Compile();

                meshCollider.enabled = true;
            }
            else
            {
#if UNITY_EDITOR
                var mesh = meshCollider.sharedMesh;
                if (mesh != null && mesh.vertexCount > 0)
                {
                    mesh.Clear();
                    meshCollider.sharedMesh = mesh;
                }
#endif
                meshCollider.enabled = false;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Implicit conversion calls <see cref="Compile(string)"/> and <see cref="Mesh.RecalculateBounds"/>.</summary>
        public static implicit operator Mesh(MeshBuilder builder)
        {
            var mesh = builder.Compile();
            mesh.RecalculateBounds();
            return mesh;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Logs all the mesh data in this <see cref="MeshBuilder"/>. Also puts the message in the system copy buffer
        /// so you can paste it into a text editor because Unity's console truncates messages that are too long.
        /// </summary>
        public void LogData(string prefix = "")
        {
            var text = WeaverUtilities.GetStringBuilder();

            text.Append(prefix);
            text.Append(" Vertices: ");
            text.Append(Vertices);
            text.AppendLine();

            if (Normals != null) text.Append(Normals);
            if (UVs != null) text.Append(UVs);
            if (UVs2 != null) text.Append(UVs2);
            if (UVs3 != null) text.Append(UVs3);
            if (UVs4 != null) text.Append(UVs4);
            if (Tangents != null) text.Append(Tangents);
            if (Colors != null) text.Append(Colors);

            for (int i = 0; i < Indices.Length; i++)
            {
                text.AppendLine();
                text.Append("Sub Mesh [");
                text.Append(i);
                text.Append("] Indices ");
                text.Append(Indices[i]);
            }

#if ! UNITY_EDITOR
            Debug.Log(text.ReleaseToString());
#else
            // The text will sometimes be longer than what Unity displays in the log,
            // so we put it in the system copy buffer for the user to paste into a text editor if they want.
            prefix = text.ToString();
            text.Release();

            Debug.Log(prefix);

            UnityEditor.EditorGUIUtility.systemCopyBuffer = prefix;
#endif
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}


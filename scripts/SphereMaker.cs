using Godot;
using System;
using System.Collections.Generic;

public class SphereMesh
{
	public readonly Vector3[] Vertices;
	public readonly int[] Indices;
	public readonly int Resolution;

	private FixedSizeList<Vector3> vertices;
	private FixedSizeList<int> indices;
	private int numDivisions;
	private int numVertsPerFace;
	
	static readonly int[] vertexPairs =
	{
		0, 1, 0, 2, 0, 3, 0, 4, 1, 2, 2, 3, 3, 4, 4, 1, 5, 1, 5, 2, 5, 3, 5, 4
	};
	static readonly int[] edgeTriplets =
	{
		0, 1, 4, 1, 2, 5, 2, 3, 6, 3, 0, 7, 8, 9, 4, 9, 10, 5, 10, 11, 6, 11, 8, 7
	};

	private static readonly Vector3[] baseVertices =
	{
		Vector3.Up, Vector3.Left, Vector3.Back, Vector3.Right, Vector3.Forward, Vector3.Down
	};

	public SphereMesh(int resolution)
	{
		this.Resolution = resolution;
		numDivisions = Mathf.Max(0, resolution);
		numVertsPerFace = ((numDivisions + 3) * (numDivisions + 3) - (numDivisions + 3)) / 2;
		int numVerts = numVertsPerFace * 8 - (numDivisions + 2) * 12 + 6;
		int numTrisPerFace = (numDivisions + 1) * (numDivisions + 1);

		vertices = new FixedSizeList<Vector3>(numVerts);
		indices = new FixedSizeList<int>(numTrisPerFace * 8 * 3);
		
		vertices.AddRange(baseVertices);

		Edge[] edges = new Edge[12];
		for (var i = 0; i < vertexPairs.Length; i += 2)
		{
			Vector3 startVertex = vertices.items[vertexPairs[i]];
			Vector3 endVertex = vertices.items[vertexPairs[i + 1]];

			int[] edgeVertexIndices = new int[numDivisions + 2];
			edgeVertexIndices[0] = vertexPairs[i];

			for (var divisionIndex = 0; divisionIndex < numDivisions; divisionIndex++)
			{
				float t = (divisionIndex + 1f) / (numDivisions + 1f);
				edgeVertexIndices[divisionIndex + 1] = vertices.nextIndex;
				vertices.Add(startVertex.Slerp(endVertex, t));
			}

			edgeVertexIndices[numDivisions + 1] = vertexPairs[i + 1];
			int edgeIndex = i / 2;
			edges[edgeIndex] = new Edge(edgeVertexIndices);
		}

		for (var i = 0; i < edgeTriplets.Length; i += 3)
		{
			int faceIndex = i / 3;
			bool reverse = faceIndex >= 4;
			CreateFace(
				edges[edgeTriplets[i]],
				edges[edgeTriplets[i+1]],
				edges[edgeTriplets[i+2]],
				reverse);
			Vertices = vertices.items;
			Indices = indices.items;
		}
	}
	void CreateFace (Edge sideA, Edge sideB, Edge bottom, bool reverse)
	{
		var numPointsInEdge = sideA.vertexIndices.Length;
		var vertexMap = new FixedSizeList<int> (numVertsPerFace);
		vertexMap.Add (sideA.vertexIndices[0]); // top of triangle

		for (var i = 1; i < numPointsInEdge - 1; i++)
		{
			vertexMap.Add (sideA.vertexIndices[i]);

			var sideAVertex = vertices.items[sideA.vertexIndices[i]];
			var sideBVertex = vertices.items[sideB.vertexIndices[i]];
			var numInnerPoints = i - 1;
			for (var j = 0; j < numInnerPoints; j++)
			{
				var t = (j + 1f) / (numInnerPoints + 1f);
				vertexMap.Add (vertices.nextIndex);
				vertices.Add (sideAVertex.Slerp(sideBVertex, t));
			}
			vertexMap.Add (sideB.vertexIndices[i]);
		}
		for (var i = 0; i < numPointsInEdge; i++)
		{
			vertexMap.Add (bottom.vertexIndices[i]);
		}
		var numRows = numDivisions + 1;
		for (var row = 0; row < numRows; row++)
		{
			var topVertex = ((row + 1) * (row + 1) - row - 1) / 2;
			var bottomVertex = ((row + 2) * (row + 2) - row - 2) / 2;

			var numTrianglesInRow = 1 + 2 * row;
			for (var column = 0; column < numTrianglesInRow; column++)
			{
				int v0, v1, v2;

				if (column % 2 == 0)
				{
					v0 = topVertex;
					v1 = bottomVertex + 1;
					v2 = bottomVertex;
					topVertex++;
					bottomVertex++;
				}
				else
				{
					v0 = topVertex;
					v1 = bottomVertex;
					v2 = topVertex - 1;
				}

				indices.Add (vertexMap.items[v0]);
				indices.Add (vertexMap.items[(reverse) ? v2 : v1]);
				indices.Add (vertexMap.items[(reverse) ? v1 : v2]);
			}
		}

	}

}

public class Edge
{
	public int[] vertexIndices;

	public Edge(int[] vertexIndices)
	{
		this.vertexIndices = vertexIndices;
	}
}

public class FixedSizeList<T>
{
	public T[] items;
	public int nextIndex;

	public FixedSizeList(int size)
	{
		items = new T[size];
	}

	public void Add(T item)
	{
		items[nextIndex] = item;
		nextIndex++;
	}

	public void AddRange(IEnumerable<T> items)
	{
		foreach (var item in items)
		{
			Add(item);
		}
	}
}

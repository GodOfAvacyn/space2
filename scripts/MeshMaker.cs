using Godot;
using System;
using System.Linq;
using Godot.Collections;
using Godot.NativeInterop;

[Tool]
public partial class MeshMaker : Node3D
{
	private static float Tolerance = 0.05f;	
	
	[Export(PropertyHint.Range, "0,100,0.05")]
	public float Resolution = 0;
	[Export(PropertyHint.Range, "1,10,")]
	public int Octaves = 0;
	[Export(PropertyHint.Range, "0,2,0.05")]
	public float Frequency = 0;
	[Export(PropertyHint.Range, "0,100,0.05")]
	public float NoiseStrength = 0;
	[Export(PropertyHint.Range, "0,5,0.05")]
	public float NoiseOffset = 0;
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float NoiseOffsetStrength = 0;
	
	private float oldResolution = 0;
	private int oldOctaves = 0;
	private float oldFrequency = 0;
	private float oldNoiseStrength = 0;
	private float oldNoiseOffset = 0;
	private float oldNoiseOffsetStrength = 0;
	

	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint()) return;
		if (Resolution == oldResolution &&
			Mathf.Abs(Frequency - oldFrequency) < Tolerance &&
			Mathf.Abs(NoiseStrength - oldNoiseStrength) < Tolerance &&
			Mathf.Abs(NoiseOffset - oldNoiseOffset) < Tolerance &&
			Mathf.Abs(NoiseOffsetStrength - oldNoiseOffsetStrength) < Tolerance)
			return;

		oldResolution = Resolution;
		oldFrequency = Frequency;
		oldNoiseStrength = NoiseStrength;
		oldNoiseOffset = NoiseOffset;
		oldNoiseOffsetStrength = NoiseOffsetStrength;

		var instance = GetNode<MeshInstance3D>("MeshInstance");
		instance.Mesh = Regenerate();

	}

	public Mesh Regenerate()
	{
		var sphere = new SphereMesh((int)Resolution);
		var indices = sphere.Indices;
		var vertices = sphere.Vertices;
		var vertexInput = vertices.SelectMany(v => new float[] { v.X, v.Y, v.Z, 0f}).ToArray();
		var numbersInput = new float[] {Octaves, Frequency, NoiseStrength, NoiseOffset, NoiseOffsetStrength};
		
		var rd = RenderingServer.CreateLocalRenderingDevice();
		var shaderFile = GD.Load<RDShaderFile>("res://compute_example.glsl");
		var shaderByteCode = shaderFile.GetSpirV();
		var shader = rd.ShaderCreateFromSpirV(shaderByteCode);
		
		var vecBytes = new byte[vertexInput.Length * sizeof(float)];
		Buffer.BlockCopy(vertexInput, 0, vecBytes, 0, vecBytes.Length);
		var vertexBuffer = rd.StorageBufferCreate((uint)vecBytes.Length, vecBytes);
		
		var numberBytes = new byte[numbersInput.Length*sizeof(float)];
		Buffer.BlockCopy(numbersInput, 0, numberBytes, 0, numberBytes.Length);
		var numberBuffer = rd.StorageBufferCreate((uint)numberBytes.Length, numberBytes);

		var vecUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 0
		};
		var numberUniform = new RDUniform
		{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1
		};
		
		vecUniform.AddId(vertexBuffer);
		numberUniform.AddId(numberBuffer);
		var uniformSet = rd.UniformSetCreate(new Array<RDUniform> { vecUniform, numberUniform }, shader, 0);
		
		// Create a compute pipeline
		var pipeline = rd.ComputePipelineCreate(shader);
		var computeList = rd.ComputeListBegin();
		rd.ComputeListBindComputePipeline(computeList, pipeline);
		rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
		rd.ComputeListDispatch(computeList, xGroups: (uint)vertices.Length, yGroups: 1, zGroups: 1);
		rd.ComputeListEnd();

		rd.Submit();
		rd.Sync();
		
		// Read back the data from the buffers
		var outputBytes = rd.BufferGetData(vertexBuffer);
		var output = new float[vertexInput.Length];
		Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);
		
		var outputVertices = new Vector3[vertexInput.Length / 4];
		for (var i = 0; i < outputVertices.Length; i++)
			outputVertices[i] = new Vector3(output[4*i], output[4*i + 1], output[4*i + 2]);
		
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		foreach (var vertex in outputVertices)
		{
			st.AddVertex(vertex);
		}
		foreach (var index in sphere.Indices)
		{
			st.AddIndex(index);	
		}
		st.GenerateNormals();
		return st.Commit();
	}
}

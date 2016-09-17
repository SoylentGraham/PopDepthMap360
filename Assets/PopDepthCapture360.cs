using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;



public class PopDepthCapture360 : MonoBehaviour {

    public Camera			SourceCamera;
	public RenderTexture	DepthEquirect;
	public Shader			DepthShader;
	public Material			BlitDepth;
	public Material			BlitEquirect;

	public RenderTexture	DepthLeft;
	public RenderTexture	DepthRight;
	public RenderTexture	DepthForward;
	public RenderTexture	DepthBackward;
	public RenderTexture	DepthUp;
	public RenderTexture	DepthDown;

	[Range(0,1000)]
    public float			BlitDepthMax = 100;
    public string			BlitDepthMaxUniform = "DepthMax";

	public RenderTexture	TempColour;
	public CameraEvent		BlitDepthAfterEvent = CameraEvent.AfterDepthTexture;
	Dictionary<RenderTexture,UnityEngine.Rendering.CommandBuffer>	PostBlitDepthCommands;

	public bool				Dirty = true;

	void Start()
	{ 
		Dirty = true;
	}

	void Update ()
	{
		if ( !Dirty )
			return;

		if ( TempColour == null )
			TempColour = new RenderTexture(1,1,24);
		

		var TempCameraObject = new GameObject ();
		//	make a camera
		var TempCamera = TempCameraObject.AddComponent<Camera>();
		
		var RotationLeft = SourceCamera.transform.rotation * Quaternion.Euler( 0, -90, 0);
		var RotationRight = SourceCamera.transform.rotation * Quaternion.Euler( 0, 90, 0);
		var RotationForward = SourceCamera.transform.rotation * Quaternion.Euler( 0, 0, 0);
		var RotationBackward = SourceCamera.transform.rotation * Quaternion.Euler( 0, 180, 0);
		var RotationUp = SourceCamera.transform.rotation * Quaternion.Euler( -90, 0, 0);
		var RotationDown = SourceCamera.transform.rotation * Quaternion.Euler( 90, 0, 0);
		 
		RenderDepth (TempCamera, RotationLeft, SourceCamera.transform.position, TempColour, ref DepthLeft);
		RenderDepth (TempCamera, RotationRight, SourceCamera.transform.position, TempColour, ref DepthRight);
		RenderDepth (TempCamera, RotationForward, SourceCamera.transform.position, TempColour, ref DepthForward);
		RenderDepth (TempCamera, RotationBackward, SourceCamera.transform.position, TempColour, ref DepthBackward);
		RenderDepth (TempCamera, RotationUp, SourceCamera.transform.position, TempColour, ref DepthUp);
		RenderDepth (TempCamera, RotationDown, SourceCamera.transform.position, TempColour, ref DepthDown);

		Destroy (TempCameraObject);

		//	make equirect
		BlitEquirect.SetTexture("CubemapLeft", DepthLeft);
		BlitEquirect.SetTexture("CubemapRight", DepthRight);
		BlitEquirect.SetTexture("CubemapFront", DepthForward);
		BlitEquirect.SetTexture("CubemapBack", DepthBackward);
		BlitEquirect.SetTexture("CubemapTop", DepthUp);
		BlitEquirect.SetTexture("CubemapBottom", DepthDown);

		Graphics.Blit( null, DepthEquirect, BlitEquirect );

		Dirty = false;
	}


	CommandBuffer GetBlitDepthCommand(RenderTexture DepthTexture)
	{
		if ( DepthTexture == null )
			DepthTexture = new RenderTexture( 1024, 1024, 0, RenderTextureFormat.ARGBFloat );

		//	get command
		if ( PostBlitDepthCommands == null )
			PostBlitDepthCommands = new Dictionary<RenderTexture, CommandBuffer>();

		if (!PostBlitDepthCommands.ContainsKey(DepthTexture))
		{
			var PostBlitDepthCommand = new CommandBuffer ();
			var Id = new RenderTargetIdentifier (DepthTexture);
			//int depthID = Shader.PropertyToID("_DepthCopyTexture"); 
			PostBlitDepthCommand.Blit(BuiltinRenderTextureType.CurrentActive, Id, BlitDepth);
			//cmd.SetGlobalTexture("_DepthBuffer", depthID);
			PostBlitDepthCommands.Add( DepthTexture, PostBlitDepthCommand );
		}

		return PostBlitDepthCommands[DepthTexture];
	}

	void RenderDepth(Camera TempCamera,Quaternion Rotation,Vector3 Position,RenderTexture ColourTexture,ref RenderTexture DepthTexture)
	{
		var PostBlitCommand = GetBlitDepthCommand( DepthTexture );
		var cam = TempCamera;
		
		cam.transform.position = Position;
		cam.transform.rotation = Rotation;
		cam.depthTextureMode = DepthTextureMode.Depth;
		cam.farClipPlane = BlitDepthMax;
		cam.fieldOfView = 90;
		cam.targetTexture = ColourTexture;

		cam.RemoveAllCommandBuffers();
		cam.AddCommandBuffer (BlitDepthAfterEvent, PostBlitCommand);
		cam.Render ();

	}


	void OnRenderImage (RenderTexture source, RenderTexture destination){

		Debug.Log ("OnRenderImage");
	
		//Graphics.Blit(source,destination,mat);
		//mat is the material which contains the shader
		//we are passing the destination RenderTexture to
	}
}

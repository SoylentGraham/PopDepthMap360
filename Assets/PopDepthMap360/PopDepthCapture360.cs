﻿using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class PopDepthCapture360 : MonoBehaviour {

	[Header("Sometimes depth is backwards on left & right. But not always. Not sure what the problem is, colour is usually fine...")]
	public bool				SwapDepthLeftRight = false;

    public Transform		SourceCamera;
	[Range(-180,180)]
	public float			YawOffset = 0;
	public int				MaxOutputWidth = 2048;
	public int				MaxOutputHeight = 2048;

	public RenderTexture	ColourEquirect;
	public RenderTexture	DepthEquirect;
	public Shader			BlitDepth;
	public Shader			BlitEquirect;
	Material				BlitDepthMaterial;
	Material				BlitEquirectMaterial;

	public RenderTexture	DepthLeft;
	public RenderTexture	DepthRight;
	public RenderTexture	DepthForward;
	public RenderTexture	DepthBackward;
	public RenderTexture	DepthUp;
	public RenderTexture	DepthDown;

	public RenderTexture	ColourLeft;
	public RenderTexture	ColourRight;
	public RenderTexture	ColourForward;
	public RenderTexture	ColourBackward;
	public RenderTexture	ColourUp;
	public RenderTexture	ColourDown;

	[Range(0,1000)]
    public float			BlitDepthMax = 100;
    public string			BlitDepthMaxUniform = "DepthMax";

	RenderTexture			TempColour;
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

		Render();

		Dirty = false;
	}

	public void Render (System.Action<Material> OnPreBlit=null)
	{
		//	reset caches. VERY IMPORTANT ON OSX (opengl?) caching materials when editing reloads nothing. presumably material caches shader blob
		if (Application.isEditor) {
			BlitDepthMaterial = null;
			BlitEquirectMaterial = null;
			PostBlitDepthCommands = null;
		}

		bool MakeDepth = ( DepthEquirect != null );
		bool MakeColour = ( ColourEquirect != null );

		//	gr: this affects depth capture
		int TempWidth = Mathf.Max( ColourEquirect ? ColourEquirect.width : 1, DepthEquirect ? DepthEquirect.width : 1 );
		int TempHeight = Mathf.Max( ColourEquirect ? ColourEquirect.height : 1, DepthEquirect ? DepthEquirect.height : 1 );
		TempWidth = Mathf.Min( MaxOutputWidth, TempWidth );
		TempHeight = Mathf.Min( MaxOutputHeight, TempHeight );
		int TempDepth = 24;

		if ( TempColour == null )
			TempColour = new RenderTexture(TempWidth,TempHeight,TempDepth);
		
		if ( BlitDepthMaterial == null )
			BlitDepthMaterial = new Material( BlitDepth );
		if ( BlitEquirectMaterial == null )
			BlitEquirectMaterial = new Material( BlitEquirect );


		if (MakeColour)
		{
			if ( ColourLeft == null )		ColourLeft = new RenderTexture( TempWidth, TempHeight, TempDepth );
			if ( ColourRight == null )		ColourRight = new RenderTexture( TempWidth, TempHeight, TempDepth );
			if ( ColourForward == null )	ColourForward = new RenderTexture( TempWidth, TempHeight, TempDepth );
			if ( ColourBackward == null )	ColourBackward = new RenderTexture( TempWidth, TempHeight, TempDepth );
			if ( ColourUp == null )			ColourUp = new RenderTexture( TempWidth, TempHeight, TempDepth );
			if ( ColourDown == null )		ColourDown = new RenderTexture( TempWidth, TempHeight, TempDepth );
		}
		else
		{
			if ( ColourLeft == null )		ColourLeft = TempColour;
			if ( ColourRight == null )		ColourRight = TempColour;
			if ( ColourForward == null )	ColourForward = TempColour;
			if ( ColourBackward == null )	ColourBackward = TempColour;
			if ( ColourUp == null )			ColourUp = TempColour;
			if ( ColourDown == null )		ColourDown = TempColour;
		}

		if (MakeDepth)
		{
			if ( DepthLeft == null )		DepthLeft = new RenderTexture( TempWidth, TempHeight, 0 );
			if ( DepthRight == null )		DepthRight = new RenderTexture( TempWidth, TempHeight, 0 );
			if ( DepthForward == null )		DepthForward = new RenderTexture( TempWidth, TempHeight, 0 );
			if ( DepthBackward == null )	DepthBackward = new RenderTexture( TempWidth, TempHeight, 0 );
			if ( DepthUp == null )			DepthUp = new RenderTexture( TempWidth, TempHeight, 0 );
			if ( DepthDown == null )		DepthDown = new RenderTexture( TempWidth, TempHeight, 0 );
		}


		var TempCameraObject = new GameObject ();
		TempCameraObject.SetActive(false);
		//	make a camera
		var TempCamera = TempCameraObject.AddComponent<Camera>();
		
		var RotationLeft = SourceCamera.transform.rotation * Quaternion.Euler( 0, YawOffset, 0) * Quaternion.Euler( 0, -90, 0);
		var RotationRight = SourceCamera.transform.rotation * Quaternion.Euler( 0, YawOffset, 0) * Quaternion.Euler( 0, 90, 0);
		var RotationForward = SourceCamera.transform.rotation * Quaternion.Euler( 0, YawOffset, 0) * Quaternion.Euler( 0, 0, 0);
		var RotationBackward = SourceCamera.transform.rotation * Quaternion.Euler( 0, YawOffset, 0) * Quaternion.Euler( 0, 180, 0);
		var RotationUp = SourceCamera.transform.rotation * Quaternion.Euler( 0, YawOffset, 0) * Quaternion.Euler( -90, 0, 0);
		var RotationDown = SourceCamera.transform.rotation * Quaternion.Euler( 0, YawOffset, 0) * Quaternion.Euler( 90, 0, 0);

		var ProgressBarTitle = "Rendering depth 360";
		var Step = 0;
		var StepCount = 6 + 1 + 1;
		System.Action<string> UpdateProgressBar = (string StepDescription) => {
			if ( EditorUtility.DisplayCancelableProgressBar( ProgressBarTitle, StepDescription, Step / (float)StepCount ) )
			{
				throw new System.Exception( ProgressBarTitle + " cancelled");
			}
			Step++;
		};

		try
		{
			UpdateProgressBar("Rendering Left");
			RenderDepth (TempCamera, RotationLeft, SourceCamera.transform.position, ColourLeft, ref DepthLeft);
			UpdateProgressBar("Rendering Right");
			RenderDepth (TempCamera, RotationRight, SourceCamera.transform.position, ColourRight, ref DepthRight);
			UpdateProgressBar("Rendering Forward");
			RenderDepth (TempCamera, RotationForward, SourceCamera.transform.position, ColourForward, ref DepthForward);
			UpdateProgressBar("Rendering Backward");
			RenderDepth (TempCamera, RotationBackward, SourceCamera.transform.position, ColourBackward, ref DepthBackward);
			UpdateProgressBar("Rendering Up");
			RenderDepth (TempCamera, RotationUp, SourceCamera.transform.position, ColourUp, ref DepthUp);
			UpdateProgressBar("Rendering Down");
			RenderDepth (TempCamera, RotationDown, SourceCamera.transform.position, ColourDown, ref DepthDown);

			if ( Application.isEditor && !Application.isPlaying )
				DestroyImmediate (TempCameraObject);
			else
				Destroy (TempCameraObject);

			//	make equirect
			if ( DepthEquirect != null )
			{
				UpdateProgressBar("Generating depth equirect");
				BlitEquirectMaterial.SetTexture("CubemapLeft", SwapDepthLeftRight ? DepthRight : DepthLeft );
				BlitEquirectMaterial.SetTexture("CubemapRight", SwapDepthLeftRight ? DepthLeft : DepthRight  );
				BlitEquirectMaterial.SetTexture("CubemapFront", DepthForward);
				BlitEquirectMaterial.SetTexture("CubemapBack", DepthBackward);
				BlitEquirectMaterial.SetTexture("CubemapTop", DepthUp);
				BlitEquirectMaterial.SetTexture("CubemapBottom", DepthDown);

				if ( OnPreBlit != null )
					OnPreBlit.Invoke( BlitEquirectMaterial );
				Graphics.Blit( null, DepthEquirect, BlitEquirectMaterial );
			}
			else
			{
				UpdateProgressBar("Generating depth equirect [skipped]");
			}

			if ( ColourEquirect != null )
			{
				UpdateProgressBar("Generating colour equirect [skipped]");
				BlitEquirectMaterial.SetTexture("CubemapLeft", ColourLeft);
				BlitEquirectMaterial.SetTexture("CubemapRight", ColourRight);
				BlitEquirectMaterial.SetTexture("CubemapFront", ColourForward);
				BlitEquirectMaterial.SetTexture("CubemapBack", ColourBackward);
				BlitEquirectMaterial.SetTexture("CubemapTop", ColourUp);
				BlitEquirectMaterial.SetTexture("CubemapBottom", ColourDown);

				if ( OnPreBlit != null )
					OnPreBlit.Invoke( BlitEquirectMaterial );
				Graphics.Blit( null, ColourEquirect, BlitEquirectMaterial );
			}
			else
			{
				UpdateProgressBar("Generating colour equirect [skipped]");
			}
			EditorUtility.ClearProgressBar();
		}
		catch
		{
			EditorUtility.ClearProgressBar();
			throw;
		}
	}


	CommandBuffer GetBlitDepthCommand(RenderTexture DepthTexture)
	{
		if ( DepthTexture == null )
			return null;

		//	get command
		if ( PostBlitDepthCommands == null )
			PostBlitDepthCommands = new Dictionary<RenderTexture, CommandBuffer>();

		if (!PostBlitDepthCommands.ContainsKey(DepthTexture))
		{
			var PostBlitDepthCommand = new CommandBuffer ();
			var Id = new RenderTargetIdentifier (DepthTexture);
			//int depthID = Shader.PropertyToID("_DepthCopyTexture"); 
			PostBlitDepthCommand.Blit(BuiltinRenderTextureType.CurrentActive, Id, BlitDepthMaterial);
			//cmd.SetGlobalTexture("_DepthBuffer", depthID);
			PostBlitDepthCommands.Add( DepthTexture, PostBlitDepthCommand );
		}

		return PostBlitDepthCommands[DepthTexture];
	}

	void RenderDepth(Camera TempCamera,Quaternion Rotation,Vector3 Position,RenderTexture ColourTexture,ref RenderTexture DepthTexture)
	{
		var cam = TempCamera;

		if ( DepthTexture != null )
		{
			DepthTexture.DiscardContents();
			Graphics.Blit( Texture2D.blackTexture, DepthTexture );
		}

		cam.transform.position = Position;
		cam.transform.rotation = Rotation;
		cam.depthTextureMode = DepthTextureMode.Depth;
		cam.nearClipPlane = 0.00001f;
		cam.farClipPlane = BlitDepthMax;
		cam.fieldOfView = 90;
		cam.targetTexture = ColourTexture;

		cam.RemoveAllCommandBuffers();

		BlitDepthMaterial.SetFloat(BlitDepthMaxUniform,BlitDepthMax);
		Matrix4x4 ScreenToViewMtx = cam.projectionMatrix.inverse;
		BlitDepthMaterial.SetVector ("ScreenToViewMtx_Row0", ScreenToViewMtx.GetRow (0));
		BlitDepthMaterial.SetVector ("ScreenToViewMtx_Row1", ScreenToViewMtx.GetRow (1));
		BlitDepthMaterial.SetVector ("ScreenToViewMtx_Row2", ScreenToViewMtx.GetRow (2));
		BlitDepthMaterial.SetVector ("ScreenToViewMtx_Row3", ScreenToViewMtx.GetRow (3));

		var PostBlitCommand = GetBlitDepthCommand( DepthTexture );
		if ( PostBlitCommand != null )
		{
			cam.AddCommandBuffer (BlitDepthAfterEvent, PostBlitCommand);
		}
		cam.Render ();

	}


	void OnRenderImage (RenderTexture source, RenderTexture destination){

		Debug.Log ("OnRenderImage");
	
		//Graphics.Blit(source,destination,mat);
		//mat is the material which contains the shader
		//we are passing the destination RenderTexture to
	}
}

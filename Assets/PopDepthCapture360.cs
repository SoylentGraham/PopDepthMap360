using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;


public class PopDepthCapture360 : MonoBehaviour {

	public Camera			SourceCamera;
	public RenderTexture	EquirectDepth;
	public RenderTexture	EquirectColour;
	public Cubemap			DepthCubemap;
	public Shader			DepthShader;
	public Material			BlitDepth;

	UnityEngine.Rendering.CommandBuffer	PostBlitDepthCommand;

	void Update () {

		if ( PostBlitDepthCommand == null )
		{
			PostBlitDepthCommand = new CommandBuffer ();
			var Id = new RenderTargetIdentifier (EquirectDepth);
			//int depthID = Shader.PropertyToID("_DepthCopyTexture"); 
			PostBlitDepthCommand.Blit(BuiltinRenderTextureType.CurrentActive, Id, BlitDepth);
			//cmd.SetGlobalTexture("_DepthBuffer", depthID);
		}

		RenderDepth (SourceCamera.transform.rotation, SourceCamera.transform.position, EquirectColour, EquirectDepth);

	}


	void RenderDepth(Quaternion Rotation,Vector3 Position,RenderTexture ColourTexture,RenderTexture DepthTexture)
	{
		var go = new GameObject ();
		//	make a camera
		var cam = go.AddComponent<Camera>();

		cam.transform.position = Position;
		cam.transform.rotation = Rotation;
		cam.depthTextureMode = DepthTextureMode.Depth;
		cam.fieldOfView = 90;
		//cam.RenderToCubemap (CubemapTexture);
		cam.targetTexture = ColourTexture;
		cam.AddCommandBuffer (UnityEngine.Rendering.CameraEvent.AfterDepthTexture, PostBlitDepthCommand);
		cam.Render ();
		//Graphics.Blit (null, DepthTexture, BlitDepth);


		Destroy (go);
	}


	void OnRenderImage (RenderTexture source, RenderTexture destination){

		Debug.Log ("OnRenderImage");
	
		//Graphics.Blit(source,destination,mat);
		//mat is the material which contains the shader
		//we are passing the destination RenderTexture to
	}
}

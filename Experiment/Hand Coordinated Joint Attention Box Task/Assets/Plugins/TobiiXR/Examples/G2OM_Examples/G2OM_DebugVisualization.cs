using System.Collections.Generic;
using Tobii.G2OM;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class G2OM_DebugVisualization : MonoBehaviour
{
    private readonly G2OM_Vector3[] _corners = new G2OM_Vector3[(int)Corners.NumberOfCorners];

    private G2OM _g2om;

    private bool _freezeVisualization = false;

    // To ensure we keep a snapshot of what G2OM did they are copies
    private G2OM_Candidate[] _candidates;
    private G2OM_CandidateResult[] _candidateResult;
    private G2OM_DeviceData _deviceData;
    private Material _mat;
    private Camera _camera;
    private Camera _mainCamera;
    private static readonly Color _lesserGreen = new Color(144 / 255f, 238 / 255f, 144 / 255f);
    private static readonly Color _perfectGreen = Color.green;

    public void ToggleVisualization()
    {
        _camera.enabled = !_camera.enabled;

        // unfreeze on disable
        if(!_camera.enabled) _freezeVisualization = false;
    }

    public void ToggleFreeze()
    {
        _freezeVisualization = !_freezeVisualization;
    }

    public void Setup(G2OM g2om, Camera mainCamera)
    {
        _g2om = g2om;
        _mat = new Material(Shader.Find("Hidden/Internal-Colored"));

        _candidates = g2om.GetCandidates();
        _candidateResult = g2om.GetCandidateResult();
        _deviceData = g2om.GetDeviceData();

        _mainCamera = mainCamera;

        _camera = GetComponent<Camera>();
        _camera.enabled = false;
    }

    private void FollowCamera()
    {
        if(_mainCamera == null) _mainCamera = GetMainCamera();
        
        if(_mainCamera == null)
        {
            Debug.LogError("No Main Camera found!", this);
            return;
        }

        transform.parent = _mainCamera.transform.parent;
        transform.position = _mainCamera.transform.position;
        transform.rotation = _mainCamera.transform.rotation;
    }

    void LateUpdate() 
    {
        if (_freezeVisualization == false)
        {
            _candidates = _g2om.GetCandidates();
            _candidateResult = _g2om.GetCandidateResult();
            _deviceData = _g2om.GetDeviceData();
        }
    }

    void OnPostRender()
    {
        if (_g2om == null)
        {
            Debug.LogWarning("G2OM visualization does not have an instance to visualize, returning.");
            return;
        }

        FollowCamera();
        Render(_mat, ref _deviceData, _candidates, _candidateResult, _corners, _freezeVisualization);
    }

    private static void Render(Material mat, ref G2OM_DeviceData deviceData, G2OM_Candidate[] g2omCandidates, G2OM_CandidateResult[] g2OmCandidatesResult, G2OM_Vector3[] corners, bool renderLeftAndRightEye)
    {
        mat.SetPass(0);

        for (var i = 0; i < g2omCandidates.Length; i++)
        {
            var g2OmCandidate = g2omCandidates[i];

            var result = Interop.G2OM_GetWorldspaceCornerOfCandidate(ref g2OmCandidate, (uint)corners.Length, corners);
            if (result != G2OM_Error.Ok)
            {
                Debug.LogError(string.Format("Failed to get corners of candidate {0}. Error code: {1}", g2OmCandidate.Id, result));
                continue;
            }

            Color resultingColor = GetResultColor(g2OmCandidatesResult, g2OmCandidate.Id);

            RenderCube(corners, resultingColor);
        }

        RenderGaze(deviceData.combined, Color.yellow);

        if(renderLeftAndRightEye)
        {
            RenderGaze(deviceData.leftEye, Color.blue);
            RenderGaze(deviceData.rightEye, Color.red);
        }
    }

    private static Color GetResultColor(G2OM_CandidateResult[] g2OmCandidatesResult, int id)
    {
        var score = 0f;
        var isFirst = false;
        for (int i = 0; i < g2OmCandidatesResult.Length; i++)
        {
            if (g2OmCandidatesResult[i].Id == id)
            {
                isFirst = i == 0;
                score = g2OmCandidatesResult[i].score;
                break;
            }
        }

        if (isFirst && score > Mathf.Epsilon) return _perfectGreen;

        return CalculateColor(score, _lesserGreen);
    }

    private static Camera GetMainCamera()
    {
        return Camera.main != null ? Camera.main : Camera.allCameras[0];
    }

    private static Color CalculateColor(float score, Color color)
    {
        return Color.Lerp(Color.white, color, score * score);
    }

    private static void RenderGaze(G2OM_GazeRay gazeRay, Color color)
    {
        var ray = gazeRay.ray;

        if (gazeRay.IsValid == false)
            return;

        GL.PushMatrix();
        GL.Begin(GL.LINES);

        GL.Color(color);
        GL.Vertex(ray.origin.Vector);
        GL.Vertex(ray.origin.Vector + ray.direction.Vector * 10);

        GL.End();
        GL.PopMatrix();
    }

    private static void RenderCube(G2OM_Vector3[] corners, Color color)
    {
        GL.PushMatrix();
        GL.Begin(GL.QUADS);

        // FRONT
        GL.Color(color);
        GL.Vertex(corners[(int)Corners.FLL].Vector);
        GL.Vertex(corners[(int)Corners.FUL].Vector);
        GL.Vertex(corners[(int)Corners.FUR].Vector);
        GL.Vertex(corners[(int)Corners.FLR].Vector);

        // LEFT SIDE
        GL.Color(color);
        GL.Vertex(corners[(int)Corners.BLL].Vector);
        GL.Vertex(corners[(int)Corners.BUL].Vector);
        GL.Vertex(corners[(int)Corners.FUL].Vector);
        GL.Vertex(corners[(int)Corners.FLL].Vector);

        GL.End();
        GL.PopMatrix();

        GL.PushMatrix();
        GL.Begin(GL.QUADS);

        // RIGHT SIDE
        GL.Color(color);

        GL.Vertex(corners[(int)Corners.FLR].Vector);
        GL.Vertex(corners[(int)Corners.FUR].Vector);
        GL.Vertex(corners[(int)Corners.BUR].Vector);
        GL.Vertex(corners[(int)Corners.BLR].Vector);

        // BOTTOM
        GL.Color(color);

        GL.Vertex(corners[(int)Corners.FLR].Vector);
        GL.Vertex(corners[(int)Corners.BLR].Vector);
        GL.Vertex(corners[(int)Corners.BLL].Vector);
        GL.Vertex(corners[(int)Corners.FLL].Vector);

        GL.End();
        GL.PopMatrix();

        GL.PushMatrix();
        GL.Begin(GL.QUADS);

        // BACK
        GL.Color(color);

        GL.Vertex(corners[(int)Corners.BLR].Vector);
        GL.Vertex(corners[(int)Corners.BUR].Vector);
        GL.Vertex(corners[(int)Corners.BUL].Vector);
        GL.Vertex(corners[(int)Corners.BLL].Vector);

        // TOP
        GL.Color(color);

        GL.Vertex(corners[(int)Corners.FUL].Vector);
        GL.Vertex(corners[(int)Corners.BUL].Vector);
        GL.Vertex(corners[(int)Corners.BUR].Vector);
        GL.Vertex(corners[(int)Corners.FUR].Vector);

        GL.End();
        GL.PopMatrix();
    }
}
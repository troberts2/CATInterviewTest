/*****************************************************************************
// File Name :         PathTracer.cs
// Author :            Tommy Roberts
// Creation Date :     4/12/25
//
// Brief Description : Holds most of the functionality for the CAT programming test
*****************************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

/// <summary>
/// holds all data and settings for path tracing. Contains UI functionality
/// </summary>
public class PathTracer : MonoBehaviour
{
    [Header("Polygons Settings")]
    [SerializeField] private Transform[] _squarePathPointTransforms;

    [SerializeField] private Transform[] _trianglePathPointTransforms;

    [SerializeField] private Transform[] _irregularPathPointTransforms;

    [SerializeField] private Transform[] _cubePathPointTransforms;

    private Transform[] _currentPointTransforms;

    private int _currentPolygonIndex = 0;

    private int _nextIndex;
    private Vector2 _currentPosition;

    [Header("Circle Settings")]
    private Vector2 _centerPoint = new Vector2(0, 2); //arbitrary middle of the other shapes value

    [SerializeField] private float _circleRadius = 2f;
    private float _angle;

    [Header("Trace Settings")]
    [SerializeField] private Transform _sphere;

    [SerializeField] private float _traceSpeed = 2f;
    
    private bool _isTracing = false;

    private bool _isReversed = false;

    [SerializeField] private Slider _traceSpeedSlider;
    
    [SerializeField] private Toggle _reverseToggle;
    [SerializeField] private Toggle _meshDrawToggle;
    [SerializeField] private Toggle _traceToggle;

    [Header("Sphere Settings")]
    [SerializeField] private GameObject _spherePrefab;
    [SerializeField] private float _sphereRadius = .5f;
    [SerializeField] private List<GameObject> _currentSpheres = new List<GameObject>();
    private float _pathLength;
    [SerializeField] private Transform _sphereHolder;

    [SerializeField] private MeshFilter mf;

    [SerializeField] private Image _addSphereButtonImage;
    [SerializeField] private Image _removeSphereButtonImage;
    [SerializeField] private Image _ballsDroppedButtonImage;

    [SerializeField] private Image _squareButton;
    [SerializeField] private Image _triangleButton;
    [SerializeField] private Image _circleButton;
    [SerializeField] private Image _irregularButton;
    [SerializeField] private Image _hightlightImage;

    public enum ShapeType
    {
        Square,
        Triangle,
        Circle,
        Irregular,
        Cube,
        None
    }

    private ShapeType _currentShapeType = ShapeType.Square;

    /// <summary>
    /// sets initial shape type to be a square
    /// </summary>
    void Start()
    {
        _currentShapeType = ShapeType.Square;
        _currentPointTransforms = _squarePathPointTransforms;
    }

    /// <summary>
    /// Calls the sphere update color function
    /// </summary>
    void Update()
    {
        UpdateSphereColors(_currentSpheres, _currentPointTransforms);
    }

    #region Adding and Removing Spheres

    /// <summary>
    /// calculates total length of current path
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private float CalculatePathLength(Transform[] path)
    {
        float length = 0;
        if(_currentShapeType == ShapeType.Circle)
        {
            length = (2 * Mathf.PI) * _circleRadius;
        }
        else
        {
            //polygon path
            for (int i = 0; i < _currentPointTransforms.Length - 1; i++)
            {
                length += Vector2.Distance(path[i].position, path[i + 1].position);
            }
            length += Vector2.Distance(path[path.Length - 1].position, path[0].position);
        }
        
        return length;
    }

    /// <summary>
    /// Gets the distance along the path at a certain path index
    /// </summary>
    /// <param name="position"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private float GetDistanceAlongPath(Vector2 position, Transform[] path)
    {
        float total = 0f;
        for (int i = 0; i < path.Length - 1; i++) 
        {
            Vector2 a = path[i].position;
            Vector2 b = path[i + 1].position;

            float segmentLength = Vector2.Distance(a, b);
            float toPoint = Vector2.Distance(a, position);
            float toEnd = Vector2.Distance(b, position);

            // Check if position is between a and b
            float dot = Vector2.Dot((position - a).normalized, (b - a).normalized);
            if (dot > 0.99f && toPoint + toEnd - segmentLength < 0.01f)
            {
                return total + toPoint;
            }

            total += segmentLength;
        }

        return total; // fallback
    }

    /// <summary>
    /// gets a color based on path completion
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    private Color GetColorFromPathProgress(float progress)
    {
        Color startColor = Color.blue;
        Color endColor = Color.red;
        return Color.Lerp(startColor, endColor, progress);
    }

    /// <summary>
    /// updates all the spheres material colors
    /// </summary>
    /// <param name="spheres"></param>
    /// <param name="path"></param>
    private void UpdateSphereColors(List<GameObject> spheres, Transform[] path)
    {
        //if tracing circle
        if(_currentShapeType == ShapeType.Circle)
        {
            foreach(var sphere in spheres)
            {
                SphereObject sphereScript = sphere.GetComponent<SphereObject>();
                float distance = sphereScript._data._angle;
                float normalizedAngle = (sphereScript._data._angle % (2 * Mathf.PI)) / (2 * Mathf.PI);
                if (normalizedAngle < 0f)
                    normalizedAngle += 1f; // ensure it's always between 0–1
                Color color = GetColorFromPathProgress(normalizedAngle);

                // Set the color (requires SpriteRenderer or Material)
                var renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = color;
            }
        }
        else
        {
            //if polygon
            foreach (var sphere in spheres)
            {
                float distance = GetDistanceAlongPath(sphere.transform.position, path);
                float t = Mathf.Clamp01(distance / _pathLength);
                Color color = GetColorFromPathProgress(t);

                // Set the color (requires SpriteRenderer or Material)
                var renderer = sphere.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = color;
            }
        }  
    }

    /// <summary>
    /// gets positions along the path at a certain length into it
    /// </summary>
    /// <param name="path"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    private Vector2 GetPositionAlongPath(Transform[] path, float length)
    {
        Vector2 position = Vector2.zero;
        float lengthWalked = 0;
        float segmentLength;
        for (int i = 0; i < path.Length - 1; i++)
        {
            segmentLength = Vector2.Distance(path[i].position, path[i + 1].position);
            lengthWalked += segmentLength;
            if ( lengthWalked >= length )
            {
                float lengthAtPrevSeg = GetLengthAtPathPosition(i);
                float distanceIntoSeg = length - lengthAtPrevSeg;
                //position must be between positions i and i + 1
                position = Vector2.Lerp(path[i].position, path[i+1].position, distanceIntoSeg / segmentLength);
                if(!_isReversed)
                {
                    _nextIndex = i + 1;
                }
                else
                {
                    _nextIndex = i;
                }

                return position;
            }
        }

        //last segment
        segmentLength = Vector2.Distance(path[path.Length - 1].position, path[0].position);
        lengthWalked += segmentLength;

        if (lengthWalked >= length)
        {
            float lengthAtPrevSeg = GetLengthAtPathPosition(path.Length - 1);
            float distanceIntoSeg = length - lengthAtPrevSeg;
            //position must be between last and first point
            position = Vector2.Lerp(path[path.Length - 1].position, path[0].position, distanceIntoSeg / segmentLength);
            if(!_isReversed)
            {
                _nextIndex = 0;
            }
            else
            {
                _nextIndex = path.Length - 1;
            }

            return position;
        }

        Debug.LogError("No position found");
        return position;
    }
    
    /// <summary>
    /// Gets the path length at the specified point
    /// </summary>
    /// <param name="pathIndex"></param>
    /// <returns></returns>
    private float GetLengthAtPathPosition(int pathIndex) 
    {
        float lengthWalked = 0;
        if (pathIndex == 0)
            return 0;
        else
        {
            for (int i = 0; i < pathIndex; i++)
            {
                lengthWalked += Vector2.Distance(_currentPointTransforms[i].position, _currentPointTransforms[i + 1].position);
                if(i+1 == pathIndex)
                {
                    return lengthWalked;
                }
            }
            lengthWalked += Vector2.Distance(_currentPointTransforms[_currentPointTransforms.Length - 1].position, _currentPointTransforms[0].position);

            if(pathIndex == _currentPointTransforms.Length - 1)
                return lengthWalked;

            Debug.LogError("index not reached");
            return -1;
        }
        
    }

    /// <summary>
    /// gets the maximum number of spheres that can fit on current path
    /// </summary>
    /// <returns></returns>
    private int GetMaxSpheres()
    {
        if(_currentPointTransforms != null || _currentShapeType == ShapeType.Circle)
        {
            _pathLength = CalculatePathLength(_currentPointTransforms);

            int maxSpheres = Mathf.FloorToInt(_pathLength / (2f * _sphereRadius));
            return maxSpheres;
        }
        else
        {
            Debug.Log("error getting max spheres");
            return -1;
        }

    }

    /// <summary>
    /// Distributes the spheres with equal distance between them along path
    /// </summary>
    private void DistributeSpheres()
    {
        _pathLength = CalculatePathLength(_currentPointTransforms);
        float distBetweenSpheres = _pathLength / _currentSpheres.Count;

        float curLengthAlongPath = 0;

        //polygon
        if(_currentShapeType != ShapeType.Circle)
        {
            for(int i = 0; i < _currentSpheres.Count; i++)
            {
                Vector2 newPos = GetPositionAlongPath(_currentPointTransforms, curLengthAlongPath);
                _currentSpheres[i].transform.position = newPos;
                _currentSpheres[i].GetComponent<SphereObject>()._data._nextIndex = _nextIndex;
                curLengthAlongPath += distBetweenSpheres;
            }
        }
        else
        {
            //circle
            for (int i = 0; i < _currentSpheres.Count; i++)
            {
                float angle = (2 * Mathf.PI / _currentSpheres.Count) * i;
                float x = Mathf.Cos(angle) * _circleRadius;
                float y = Mathf.Sin(angle) * _circleRadius;
                _currentSpheres[i].transform.position = new Vector2(_centerPoint.x + x, _centerPoint.y - y);
                _currentSpheres[i].GetComponent<SphereObject>()._data._angle = angle;
            }
        }
        
    }

    /// <summary>
    /// Adds a sphere to the mix
    /// </summary>
    public void AddSphere()
    {
        if(GetMaxSpheres() <= _currentSpheres.Count)
        {
            Debug.Log("Sphere max already reached");
            _addSphereButtonImage.color = Color.red;
            _addSphereButtonImage.DOColor(Color.white, .5f);
            return;
        }
        GameObject newSphere = Instantiate(_spherePrefab, _sphereHolder);
        newSphere.transform.localScale = Vector3.zero;
        newSphere.transform.DOScale(Vector3.one * _sphereRadius * 2, .2f).SetEase(Ease.InSine);
        _currentSpheres.Add(newSphere);
        SphereObject sphere = newSphere.GetComponent<SphereObject>();
        sphere.SetSphereData(_currentPointTransforms, _currentPolygonIndex, _nextIndex, _currentPosition, _angle, _traceSpeed,
            _circleRadius, _centerPoint, _isTracing, _isReversed, _currentShapeType);

        DistributeSpheres();
    }

    /// <summary>
    /// removes a sphere from the mix
    /// </summary>
    public void RemoveSphere()
    {
        if(_currentSpheres.Count <= 0)
        {
            Debug.Log("No spheres to remove");
            _removeSphereButtonImage.color = Color.red;
            _removeSphereButtonImage.DOColor(Color.white, .5f);
            return;
        }

        _currentSpheres[_currentSpheres.Count - 1].transform.DOScale(Vector3.zero, .2f).SetEase(Ease.OutSine);
        Invoke(nameof(DestroyLastSphere), .2f);

        DistributeSpheres();
    }

    /// <summary>
    /// Destroys the last sphere in the current sphere list
    /// </summary>
    private void DestroyLastSphere()
    {
        Destroy(_currentSpheres[_currentSpheres.Count - 1]);
        _currentSpheres.RemoveAt(_currentSpheres.Count - 1);
    }

    #endregion

    #region Trace UI

    /// <summary>
    /// changes trace speed of spheres according to the slider
    /// </summary>
    public void OnSpeedSliderChanged()
    {
        if(_traceSpeedSlider != null)
        {
            _traceSpeed = _traceSpeedSlider.value;
            foreach (GameObject sphere in _currentSpheres)
            {
                SphereObject sphereScript = sphere.GetComponent<SphereObject>();
                sphereScript._data._traceSpeed = _traceSpeed;
            }
        }
    }

    /// <summary>
    /// changes reverse or not based on UI toggle
    /// </summary>
    public void OnReverseToggle()
    {
        if(_reverseToggle != null)
        {
            _isReversed = !_isReversed;

            foreach(GameObject sphere in _currentSpheres)
            {
                SphereObject sphereScript = sphere.GetComponent<SphereObject>();
                sphereScript._data._isReversed = _isReversed;

                //if tracing polygons
                if (_currentPointTransforms != null)
                {
                    if (_isReversed)
                    {
                        sphereScript._data._nextIndex--;
                        if (sphereScript._data._nextIndex < 0)
                            sphereScript._data._nextIndex = sphereScript._data._currentPointTransforms.Length - 1;

                        sphereScript._data._nextPoint = _currentPointTransforms[sphereScript._data._nextIndex].position;
                    }
                    else
                    {
                        sphereScript._data._nextIndex++;
                        if (sphereScript._data._nextIndex > sphereScript._data._currentPointTransforms.Length - 1)
                            sphereScript._data._nextIndex = 0;

                        sphereScript._data._nextPoint = _currentPointTransforms[sphereScript._data._nextIndex].position;
                    }
                }
            }  
        }
    }

    /// <summary>
    /// Changes whether app draws mesh based on toggle
    /// </summary>
    public void OnMeshDrawToggle()
    {
        if(_meshDrawToggle != null)
        {
            if( _meshDrawToggle.isOn)
            {
                if (_currentShapeType != ShapeType.Circle)
                {
                    Vector2[] points = _currentPointTransforms.Select(t => (Vector2)t.position).ToArray();

                    mf.mesh = Generate2DMesh(points);
                }
                else
                {
                    //is circle
                    mf.mesh = Generate2DMesh(GenerateCirclePoints(_centerPoint, _circleRadius, 36));
                } 
            }
            else
            {
                mf.mesh.Clear();
                mf.mesh = new Mesh();
            }
        }
    }

    /// <summary>
    /// Starts the coroutine for dropping the balls off screen
    /// </summary>
    public void OnBallsDropped()
    {
        if(_ballsDropped == null && _currentSpheres.Count > 0)
            _ballsDropped = StartCoroutine(DropBalls());
        else
        {
            _ballsDroppedButtonImage.color = Color.red;
            _ballsDroppedButtonImage.DOColor(Color.white, .5f);
        }
    }

    private Coroutine _ballsDropped;
    /// <summary>
    /// Drops the balls off the screen and returns them after a few seconds
    /// </summary>
    /// <returns></returns>
    private IEnumerator DropBalls()
    {
        foreach(GameObject sphere in _currentSpheres)
        {
            SphereObject sphereScript = sphere.GetComponent<SphereObject>();
            sphereScript._data._isTracing = false;
            Vector2 endPos = new Vector2(sphere.transform.position.x, -15f);
            sphere.transform.DOMove(endPos, 1f, false).SetEase(Ease.InBack);
            yield return new WaitForSeconds(.1f);
        }
        yield return new WaitForSeconds(3f);

        foreach (GameObject sphere in _currentSpheres)
        {
            SphereObject sphereScript = sphere.GetComponent<SphereObject>();
            sphereScript._data._isTracing = true;
        }

        DistributeSpheres();
        _ballsDropped = null;
    }

    #endregion

    #region Trace Functionality

    /// <summary>
    /// sets the current path to whatever button was clicked
    /// </summary>
    /// <param name="shape"></param>
    private void TracePath(ShapeType shape)
    {
        switch (shape)
        {
            case ShapeType.Square:
                _currentPointTransforms = _squarePathPointTransforms;
                break;
            case ShapeType.Triangle:
                _currentPointTransforms = _trianglePathPointTransforms;
                break;
            case ShapeType.Irregular:
                _currentPointTransforms = _irregularPathPointTransforms;
                break;
            case ShapeType.Cube:
                _currentPointTransforms = _cubePathPointTransforms;
                break;
            case ShapeType.Circle:
                _currentPointTransforms = null;
                break;
        }

        foreach (GameObject sphere in _currentSpheres)
        {
            SphereObject sphereScript = sphere.GetComponent<SphereObject>();

            sphereScript._data._currentShapeType = shape;
            sphereScript._data._currentPointTransforms = _currentPointTransforms;
        }
        DistributeSpheres();

        if(_meshDrawToggle.isOn)
        {
            if(_currentShapeType != ShapeType.Circle)
            {
                Vector2[] points = _currentPointTransforms.Select(t => (Vector2)t.position).ToArray();

                mf.mesh = Generate2DMesh(points);
            }
            else
            {
                //is a circle
                mf.mesh = Generate2DMesh(GenerateCirclePoints(_centerPoint, _circleRadius, 36));
            }
            
        }
        else
        {
            mf.mesh.Clear();
            mf.mesh = new Mesh();
        }
    }

    /// <summary>
    /// enables or disables the spheres from moving based on toggle value
    /// </summary>
    public void OnTraceToggle()
    {
        if(_traceToggle != null)
        {
            if(_traceToggle.isOn)
            {
                _isTracing = true;
            }
            else
            {
                _isTracing = false;
            }
            foreach (GameObject sphere in _currentSpheres)
            {
                SphereObject sphereScript = sphere.GetComponent<SphereObject>();
                sphereScript._data._isTracing = _isTracing;
            }
        }
    }

    #endregion

    #region Buttons In Unity

    /// <summary>
    /// sets path to square
    /// </summary>
    public void SquareButton()
    {
        _currentShapeType = ShapeType.Square;
        TracePath(_currentShapeType);
        MoveImageAOnTopOfImageB(_hightlightImage.GetComponent<RectTransform>(), _squareButton.GetComponent<RectTransform>());
        PruneSpheres();
    }

    /// <summary>
    /// sets path to triangle
    /// </summary>
    public void TriangleButton()
    {
        _currentShapeType = ShapeType.Triangle;
        TracePath(_currentShapeType);
        MoveImageAOnTopOfImageB(_hightlightImage.GetComponent<RectTransform>(), _triangleButton.GetComponent<RectTransform>());
        PruneSpheres();
    }

    /// <summary>
    /// sets path to circle
    /// </summary>
    public void CircleButton()
    {
        _currentShapeType = ShapeType.Circle;
        TracePath(_currentShapeType);
        MoveImageAOnTopOfImageB(_hightlightImage.GetComponent<RectTransform>(), _circleButton.GetComponent<RectTransform>());
        PruneSpheres();
    }

    /// <summary>
    /// sets path to irregular shape
    /// </summary>
    public void IrregularButton()
    {
        _currentShapeType = ShapeType.Irregular;
        TracePath(_currentShapeType);
        MoveImageAOnTopOfImageB(_hightlightImage.GetComponent<RectTransform>(), _irregularButton.GetComponent<RectTransform>());
        PruneSpheres();
    }

    /// <summary>
    /// quits the app
    /// </summary>
    public void QuitApplication()
    {
        Application.Quit();
    }

    /// <summary>
    /// prunes spheres if there are too many for current path length
    /// </summary>
    private void PruneSpheres()
    {
        if (GetMaxSpheres() < _currentSpheres.Count)
        {
            int removedCount = 0;
            Debug.Log("pruned");
            for (int i = GetMaxSpheres(); i < _currentSpheres.Count; i++)
            {
                Destroy(_currentSpheres[i]);
                _currentSpheres.RemoveAt(i);
                removedCount++;
            }

            Debug.Log("max sphhere: " + GetMaxSpheres() + " pruned: " + removedCount);
            DistributeSpheres();
        }
    }

    /// <summary>
    /// generates an array of points around the traced circle
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    Vector2[] GenerateCirclePoints(Vector2 center, float radius, int count = 36)
    {
        Vector2[] points = new Vector2[count];
        float angleStep = 2 * Mathf.PI / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            points[i] = new Vector2(center.x + x, center.y - y); 
        }

        return points;
    }

    /// <summary>
    /// Generates mesh based on path points
    /// </summary>
    /// <param name="points"></param>
    /// <returns></returns>
    private static Mesh Generate2DMesh(Vector2[] points)
    {
        Mesh mesh = new Mesh();

        // Convert Vector2s to Vector3s
        Vector3[] vertices = points.Select(p => new Vector3(p.x, p.y, 0f)).ToArray();

        // Generate triangle fan (assumes points form a convex shape)
        List<int> triangles = new List<int>();
        for (int i = 1; i < points.Length - 1; i++)
        {
            triangles.Add(0);      // center point
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        Vector2[] uvs = points;

        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// moves highlight image on top of currently selected path button
    /// </summary>
    /// <param name="imageA"></param>
    /// <param name="imageB"></param>
    private void MoveImageAOnTopOfImageB(RectTransform imageA, RectTransform imageB)
    {
        // Get screen position of imageB
        Vector3 worldPosB = imageB.position;

        // Convert world position to local position in imageA's parent
        RectTransform parentA = imageA.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentA,
            RectTransformUtility.WorldToScreenPoint(null, worldPosB),
            null,
            out Vector2 localPoint))
        {
            imageA.localPosition = localPoint;
        }
    }

    #endregion
}

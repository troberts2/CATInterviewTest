using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PathTracer : MonoBehaviour
{
    [Header("Polygons Settings")]
    [SerializeField] private Transform[] _squarePathPointTransforms;

    [SerializeField] private Transform[] _trianglePathPointTransforms;

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

    [Header("Sphere Settings")]
    [SerializeField] private GameObject _spherePrefab;
    [SerializeField] private float _sphereRadius = .5f;
    [SerializeField] private List<GameObject> _currentSpheres = new List<GameObject>();
    private float _pathLength;
    [SerializeField] private Transform _sphereHolder;


    public enum ShapeType
    {
        Square,
        Triangle,
        Circle,
        None
    }

    private ShapeType _currentShapeType = ShapeType.None;

    // Start is called before the first frame update
    void Start()
    {
        //AddSphere();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSphereColors(_currentSpheres, _currentPointTransforms);
    }

    #region Adding and Removing Spheres

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

    private Color GetColorFromPathProgress(float progress)
    {
        Color startColor = Color.blue;
        Color endColor = Color.red;
        return Color.Lerp(startColor, endColor, progress);
    }

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

    public void AddSphere()
    {
        if(GetMaxSpheres() <= _currentSpheres.Count)
        {
            Debug.Log("Sphere max already reached");
            return;
        }
        GameObject newSphere = Instantiate(_spherePrefab, _sphereHolder);
        _currentSpheres.Add(newSphere);
        SphereObject sphere = newSphere.GetComponent<SphereObject>();
        sphere.SetSphereData(_currentPointTransforms, _currentPolygonIndex, _nextIndex, _currentPosition, _angle, _traceSpeed,
            _circleRadius, _centerPoint, _isTracing, _isReversed, _currentShapeType);

        DistributeSpheres();
    }

    #endregion

    #region Trace UI

    public void OnSpeedSliderChanged()
    {
        if(_traceSpeedSlider != null)
        {
            foreach (GameObject sphere in _currentSpheres)
            {
                SphereObject sphereScript = sphere.GetComponent<SphereObject>();
                sphereScript._data._traceSpeed = _traceSpeedSlider.value;
            }
        }
    }

    public void OnReverseToggle()
    {
        if(_reverseToggle != null)
        {
            _isReversed = !_isReversed;

            foreach(GameObject sphere in _currentSpheres)
            {
                SphereObject sphereScript = sphere.GetComponent<SphereObject>();

                //if tracing polygons
                if (_currentPointTransforms != null)
                {
                    if (_isReversed)
                    {
                        sphereScript._data._currentPolygonIndex--;
                        if (sphereScript._data._currentPolygonIndex < 0)
                            sphereScript._data._currentPolygonIndex = sphereScript._data._currentPointTransforms.Length - 1;
                    }
                    else
                    {
                        sphereScript._data._currentPolygonIndex++;
                        if (sphereScript._data._currentPolygonIndex > sphereScript._data._currentPointTransforms.Length - 1)
                            sphereScript._data._currentPolygonIndex = 0;
                    }
                    sphereScript._data._nextIndex = sphereScript._data._currentPolygonIndex;
                }
            }  
        }
    }

    public void OnBallsDropped()
    {
        StartCoroutine(DropBalls());
    }

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
    }

    #endregion

    #region Trace Functionality

    private void TracePath(ShapeType shape)
    {
        _isTracing = true;
        switch (shape)
        {
            case ShapeType.Square:
                _currentPointTransforms = _squarePathPointTransforms;
                break;
            case ShapeType.Triangle:
                _currentPointTransforms = _trianglePathPointTransforms;
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
    }

    #endregion

    #region Buttons In Unity

    public void SquareButton()
    {
        _currentShapeType = ShapeType.Square;
        TracePath(_currentShapeType);
        PruneSpheres();
    }

    public void TriangleButton()
    {
        _currentShapeType = ShapeType.Triangle;
        TracePath(_currentShapeType);
        PruneSpheres();
    }

    public void CircleButton()
    {
        _currentShapeType = ShapeType.Circle;
        TracePath(_currentShapeType);
        PruneSpheres();
    }

    private void PruneSpheres()
    {
        if (GetMaxSpheres() < _currentSpheres.Count)
        {
            Debug.Log("pruned");
            for (int i = GetMaxSpheres(); i < _currentSpheres.Count; i++)
            {
                Destroy(_currentSpheres[i]);
                _currentSpheres.RemoveAt(i);
            }
        }
    }

    #endregion
}

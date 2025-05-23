/*****************************************************************************
// File Name :         PathTracer.cs
// Author :            Tommy Roberts
// Creation Date :     4/12/25
//
// Brief Description : Holds the functionality for the spheres
*****************************************************************************/
using UnityEngine;

/// <summary>
/// holds data and functionality of the spheres
/// </summary>
public class SphereObject : MonoBehaviour
{
    /// <summary>
    /// holds all of the data and settings for the sphere
    /// </summary>
    public struct SphereData
    {
        public Transform[] _currentPointTransforms;

        public int _currentPolygonIndex;

        public int _nextIndex;
        public Vector2 _currentPosition;

        public float _angle;
        public float _traceSpeed;

        public float _circleRadius;

        public Vector2 _centerPoint;

        public bool _isTracing;

        public bool _isReversed;

        public Vector2 _nextPoint;

        public PathTracer.ShapeType _currentShapeType;
    }

    public SphereData _data;

    /// <summary>
    /// Sets the sphere data 
    /// </summary>
    /// <param name="currentPointTransforms"></param>
    /// <param name="index"></param>
    /// <param name="nextIndex"></param>
    /// <param name="currentPos"></param>
    /// <param name="angle"></param>
    /// <param name="traceSpeed"></param>
    /// <param name="circleRadius"></param>
    /// <param name="centerPoint"></param>
    /// <param name="isTracing"></param>
    /// <param name="isReversed"></param>
    /// <param name="currentShape"></param>
    public void SetSphereData(Transform[] currentPointTransforms, int index, int nextIndex, Vector2 currentPos, float angle, float traceSpeed,
        float circleRadius, Vector2 centerPoint, bool isTracing, bool isReversed, PathTracer.ShapeType currentShape)
    {
        _data._currentPointTransforms = currentPointTransforms;
        _data._currentPolygonIndex = index;
        _data._nextIndex = nextIndex;
        _data._currentPosition = currentPos;
        _data._angle = angle;
        _data._traceSpeed = traceSpeed;
        _data._circleRadius = circleRadius;
        _data._centerPoint = centerPoint;
        _data._isTracing = isTracing;
        _data._isReversed = isReversed;
        _data._currentShapeType = currentShape;
    }


    /// <summary>
    /// creates an instance of the renderers material so all colors arent the same
    /// </summary>
    void Start()
    {
        Renderer r = GetComponent<Renderer>();
        r.material = new Material(r.material); // clone material
    }

    /// <summary>
    /// calls the trace shape functions
    /// </summary>
    void Update()
    {
        if (_data._isTracing)
        {
            if (_data._currentShapeType == PathTracer.ShapeType.Square || _data._currentShapeType == PathTracer.ShapeType.Triangle || _data._currentShapeType == PathTracer.ShapeType.Irregular || _data._currentShapeType == PathTracer.ShapeType.Cube)
                TracePolygon();
            else if (_data._currentShapeType == PathTracer.ShapeType.Circle)
                TraceCircle();
        }
    }

    /// <summary>
    /// trace function for any polygon
    /// </summary>
    private void TracePolygon()
    {

        _data._nextPoint = _data._currentPointTransforms[_data._nextIndex].position;
        _data._currentPosition = transform.position;

        //move target sphere toward next point
        transform.position = Vector3.MoveTowards(transform.position, _data._nextPoint, _data._traceSpeed * Time.deltaTime);

        //check if we've reached next point
        if (Vector2.Distance(transform.position, _data._nextPoint) < 0.01f)
        {
            if (!_data._isReversed)
            {
                _data._nextIndex++;

                //reset path before going out of bounds
                if (_data._nextIndex > _data._currentPointTransforms.Length - 1)
                    _data._nextIndex = 0;
            }
            else
            {
                _data._nextIndex--;

                //reset path before going out of bounds
                if (_data._nextIndex < 0)
                    _data._nextIndex = _data._currentPointTransforms.Length - 1;
            }

            _data._nextPoint = _data._currentPointTransforms[_data._nextIndex].position;
        }
    }

    /// <summary>
    /// trace function for circle
    /// </summary>
    private void TraceCircle()
    {
        if (!_data._isReversed)
            _data._angle += _data._traceSpeed * Time.deltaTime;
        else
            _data._angle -= _data._traceSpeed * Time.deltaTime;


        float x = Mathf.Cos(_data._angle) * _data._circleRadius;
        float y = Mathf.Sin(_data._angle) * _data._circleRadius;
        transform.position = new Vector2(_data._centerPoint.x + x, _data._centerPoint.y - y);
    }
}

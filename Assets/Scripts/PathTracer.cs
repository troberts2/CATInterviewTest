using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PathTracer : MonoBehaviour
{
    [Header("Polygons Settings")]
    [SerializeField] private Transform[] _squarePathPointTransforms;

    [SerializeField] private Transform[] _trianglePathPointTransforms;

    private Transform[] _currentPointTransforms;

    private int _currentPolygonIndex = 0;

    private Vector2 _nextPoint;
    private Vector2 _currentPosition;

    [Header("Circle Settings")]
    private Vector2 _centerPoint = new Vector2(0, 2); //arbitrary middle of the other shapes value

    [SerializeField] private float _circleRadius = 2f;
    private float _angle;

    [Header("Trace Settings")]
    [SerializeField] private Transform _sphere;

    [SerializeField] private float _traceSpeed = 2f;
    
    private bool _isTracing = false;

    [SerializeField] private Slider _traceSpeedSlider;
    private bool _isReversed = false;
    [SerializeField] private Toggle _reverseToggle;

    private enum ShapeType
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


    }

    // Update is called once per frame
    void Update()
    {
        if(_isTracing)
        {
            if (_currentShapeType == ShapeType.Square || _currentShapeType == ShapeType.Triangle)
                TracePolygon();
            else if (_currentShapeType == ShapeType.Circle)
                TraceCircle();
        }
    }

    #region Trace UI

    public void OnSpeedSliderChanged()
    {
        if(_traceSpeedSlider != null)
        {
            _traceSpeed = _traceSpeedSlider.value;
        }
    }

    public void OnReverseToggle()
    {
        if(_reverseToggle != null)
        {
            _isReversed = !_isReversed;

            //if tracing polygons
            if(_currentPointTransforms != null)
            {
                if (_isReversed)
                {
                    _currentPolygonIndex--;
                    if (_currentPolygonIndex < 0)
                        _currentPolygonIndex = _currentPointTransforms.Length - 1;
                }
                else
                {
                    _currentPolygonIndex++;
                    if(_currentPolygonIndex > _currentPointTransforms.Length - 1)
                        _currentPolygonIndex = 0;
                }

                _nextPoint = _currentPointTransforms[_currentPolygonIndex].position;
            }
        }
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
    }


    private void TracePolygon()
    {

        _nextPoint = _currentPointTransforms[_currentPolygonIndex].position;
        _currentPosition = _sphere.position;

        //move target sphere toward next point
        _sphere.position = Vector2.MoveTowards(_currentPosition, _nextPoint, _traceSpeed * Time.deltaTime);

        //check if we've reached next point
        if(Vector2.Distance(_currentPosition, _nextPoint) < 0.01f)
        {
            if(!_isReversed)
            {
                _currentPolygonIndex++;

                //reset path before going out of bounds
                if (_currentPolygonIndex > _currentPointTransforms.Length - 1)
                    _currentPolygonIndex = 0;
            }
            else
            {
                _currentPolygonIndex--;

                //reset path before going out of bounds
                if (_currentPolygonIndex < 0)
                    _currentPolygonIndex = _currentPointTransforms.Length - 1;
            }
            
        }
    }

    private void TraceCircle()
    {
        if (!_isReversed)
            _angle += _traceSpeed * Time.deltaTime;
        else
            _angle -= _traceSpeed * Time.deltaTime;

        float x = Mathf.Cos(_angle) * _circleRadius;
        float y = Mathf.Sin(_angle) * _circleRadius;
        _sphere.position = new Vector2(_centerPoint.x + x, _centerPoint.y - y);
    }

    #endregion

    #region Buttons In Unity

    public void SquareButton()
    {
        _currentShapeType = ShapeType.Square;
        TracePath(_currentShapeType);
    }

    public void TriangleButton()
    {
        _currentShapeType = ShapeType.Triangle;
        TracePath(_currentShapeType);
    }

    public void CircleButton()
    {
        _currentShapeType = ShapeType.Circle;
        TracePath(_currentShapeType);
    }

    #endregion
}

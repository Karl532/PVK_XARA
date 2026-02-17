using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class WorkspaceMovementState : MonoBehaviour
{
    [Header("Movement Detection")]
    [SerializeField] private float positionEpsilon = 0.001f;
    [SerializeField] private float rotationEpsilonDegrees = 0.1f;
    [SerializeField] private float scaleEpsilon = 0.001f;
    [SerializeField] private float moveCooldownSeconds = 0.1f;

    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private Vector3 _lastScale;
    private bool _hasLast;
    private float _lastMoveTime = -999f;
    private bool _grabbed;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable _interactable;

    public float LastMoveTime => _lastMoveTime;
    public bool IsMoving => _grabbed || (Time.unscaledTime - _lastMoveTime <= moveCooldownSeconds);

    private void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
    }

    private void OnEnable()
    {
        CacheTransform();

        if (_interactable != null)
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        if (_interactable != null)
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    public void MarkMoved()
    {
        _lastMoveTime = Time.unscaledTime;
    }

    private void LateUpdate()
    {
        if (!_hasLast)
        {
            CacheTransform();
            return;
        }

        var t = transform;
        bool moved = false;

        if ((_lastPosition - t.position).sqrMagnitude > positionEpsilon * positionEpsilon)
            moved = true;

        if (Quaternion.Angle(_lastRotation, t.rotation) > rotationEpsilonDegrees)
            moved = true;

        if ((_lastScale - t.localScale).sqrMagnitude > scaleEpsilon * scaleEpsilon)
            moved = true;

        if (moved)
            _lastMoveTime = Time.unscaledTime;

        _lastPosition = t.position;
        _lastRotation = t.rotation;
        _lastScale = t.localScale;
    }

    private void CacheTransform()
    {
        var t = transform;
        _lastPosition = t.position;
        _lastRotation = t.rotation;
        _lastScale = t.localScale;
        _hasLast = true;
    }

    private void OnSelectEntered(SelectEnterEventArgs _)
    {
        _grabbed = true;
        _lastMoveTime = Time.unscaledTime;
    }

    private void OnSelectExited(SelectExitEventArgs _)
    {
        _grabbed = false;
        _lastMoveTime = Time.unscaledTime;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputController : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public static Vector2 MovementInput = Vector2.zero;
    public static Vector2 CameraInput = Vector2.zero;

    public enum JoystickType { Movement, Camera }
    
    [Header("Настройки Сенсора")]
    public JoystickType joystickType;
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    
    private float joystickRadius;

    void Start()
    {
        if (joystickBackground != null)
        {
            joystickRadius = joystickBackground.sizeDelta.x / 2f;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (joystickBackground == null || joystickHandle == null) return;

        Vector2 position;
        bool isCanvasValid = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, 
            eventData.position, 
            eventData.pressEventCamera, 
            out position
        );

        if (isCanvasValid == true)
        {
            position.x = position.x / joystickRadius;
            position.y = position.y / joystickRadius;

            Vector2 clampedPosition = position;
            if (clampedPosition.magnitude > 1f)
            {
                clampedPosition = clampedPosition.normalized;
            }

            joystickHandle.anchoredPosition = new Vector2(
                clampedPosition.x * joystickRadius,
                clampedPosition.y * joystickRadius
            );

            if (joystickType == JoystickType.Movement)
            {
                MovementInput = clampedPosition;
            }
            else if (joystickType == JoystickType.Camera)
            {
                CameraInput = clampedPosition * 2f; 
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (joystickHandle != null)
        {
            joystickHandle.anchoredPosition = Vector2.zero;
        }

        if (joystickType == JoystickType.Movement)
        {
            MovementInput = Vector2.zero;
        }
        else if (joystickType == JoystickType.Camera)
            CameraInput = Vector2.zero;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class MinecraftJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public static Vector2 InputDirection { get; private set; } = Vector2.zero;

    private RectTransform baseRect;
    private RectTransform handleRect;
    private float movementRange;

    void Start()
    {
        baseRect = GetComponent<RectTransform>();
        
        if (transform.childCount > 0)
        {
            handleRect = transform.GetChild(0).GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("[Joystick] Ошибка! Внутри Minecraft_Joystick_Base нет дочернего объекта Handle!");
        }
        
        movementRange = baseRect.sizeDelta.x / 2f;
        if (movementRange <= 0) movementRange = 50f;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (baseRect == null || handleRect == null) return;

        Vector2 position;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out position))
        {
            position.x = Mathf.Clamp(position.x, -movementRange, movementRange);
            position.y = Mathf.Clamp(position.y, -movementRange, movementRange);

            handleRect.anchoredPosition = position;
            InputDirection = new Vector2(position.x / movementRange, position.y / movementRange);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData); 
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputDirection = Vector2.zero;
        if (handleRect != null)
        {
            handleRect.anchoredPosition = Vector2.zero;
        }
    }
}

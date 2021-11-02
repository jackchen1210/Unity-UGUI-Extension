using UnityEngine;
using UnityEngine.UI;
#if ENABLE_DOTWEEN
using DG.Tweening;
#endif
using System;
using UnityEngine.EventSystems;
using System.Linq;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectHelper<T> : MonoBehaviour, IBeginDragHandler, IEndDragHandler where T : MonoBehaviour
{
    public Action<T> OnFocusElement { get; set; }
    [SerializeField] private float stopOnVelocity = 200;
    [SerializeField] private bool isCenterOnChild = true;
    [SerializeField] private T[] elements;
#if ENABLE_DOTWEEN
    [Header("Tween Setting")]
    [SerializeField] private bool enableTween;
    [SerializeField] private float tweenDuraction = 0.1f;
    [SerializeField] private Ease tweenEase;
    private Tween tween;
#endif

    private bool isDragging;
    private ScrollRect scrollRect;
    private RectTransform contentRect;
    private Content content;

    private void Awake()
    {
        content = new Content(elements);
        scrollRect = GetComponent<ScrollRect>();
        contentRect = scrollRect.content;
        scrollRect.onValueChanged.AddListener(OnValueChanged);
    }
    
    private void OnValueChanged(Vector2 data)
    {
        if(scrollRect.velocity != Vector2.zero)
        {
            CheckGotoCloseElement();
        }
    }
    

    private void CheckGotoCloseElement()
    {
        if (!isCenterOnChild)
        {
            return;
        }

        if (isDragging)
        {
            return;
        }

        if (CheckVelocity())
        {
            scrollRect.velocity = Vector2.zero;
            var element = content.GetClosestElement(transform.position);

            var pos = GetCenteredContentPosition(element);

#if ENABLE_DOTWEEN
            if (enableTween &&(tween == null || !tween.IsActive()))
            {
                tween = contentRect.transform
                    .DOMove(pos, tweenDuraction)
                    .SetEase(tweenEase)
                    .OnComplete(() => OnFocusElement?.Invoke(element))
                    .OnKill(() => tween = null);
                return;
            }
#endif
            contentRect.transform.position = pos;
        }
    }

    private bool CheckVelocity()
    {
        var verticalCheck = true;
        var horizontalCheck = true;
        if (scrollRect.vertical)
        {
            verticalCheck = Mathf.Abs(scrollRect.velocity.y) <= stopOnVelocity;
        }
        if (scrollRect.horizontal)
        {
            horizontalCheck = Mathf.Abs(scrollRect.velocity.x) <= stopOnVelocity;
        }
        return verticalCheck && horizontalCheck;
    }


    /// <see href=""="https://forum.unity.com/threads/scrollrect-center-on-child.279040/">Source code</see>
    /// <param name="child"></param>
    /// <param name="scrollRect"></param>
    /// <returns></returns>
    public Vector3 GetCenteredContentPosition(T child)
    {
        Vector3[] viewportCorners = new Vector3[4];
        RectTransform viewport = scrollRect.viewport;
        viewport = viewport != null ? viewport : (RectTransform)scrollRect.transform;
        viewport.GetWorldCorners(viewportCorners);
        var centreWorldPosY = ((viewportCorners[1].y - viewportCorners[0].y) / 2f) + viewportCorners[0].y;
        var centreWorldPosX = ((viewportCorners[2].x - viewportCorners[1].x) / 2f) + viewportCorners[1].x;
        var centreWorldPos = new Vector3(centreWorldPosX, centreWorldPosY, 0);
        float h = centreWorldPos.y - child.transform.position.y;
        float w = centreWorldPos.x - child.transform.position.x;
        Vector3 displacement = new Vector3(w, h, 0);
        Vector3[] contentCorners = new Vector3[4];
        scrollRect.content.GetWorldCorners(contentCorners);

        if (contentCorners[1].y + displacement.y < viewportCorners[1].y)
        {
            displacement.y = viewportCorners[1].y - contentCorners[1].y;
        }
        else if (contentCorners[0].y + displacement.y > viewportCorners[0].y)
        {
            displacement.y = viewportCorners[0].y - contentCorners[0].y;
        }
        if (contentCorners[2].x + displacement.x < viewportCorners[2].x)
        {
            displacement.x = viewportCorners[2].x - contentCorners[2].x;
        }
        else if (contentCorners[1].x + displacement.x > viewportCorners[1].x)
        {
            displacement.x = viewportCorners[1].x - contentCorners[1].x;
        }

        return scrollRect.content.position + displacement;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        CheckGotoCloseElement();
    }


    private class Content
    {
        private T elementCache;
        private T[] elements;
        private T firstElement;
        private T lastElement;

        public Content(T[] elements)
        {
            this.elements = elements;
            firstElement = elements.First();
            lastElement = elements.Last();
        }

        internal T GetClosestElement(Vector3 viewPos)
        {
            elementCache = elements.OrderBy(_ => Vector3.Distance(_.transform.position, viewPos)).First();

            return elementCache;
        }

        internal bool IsAtBoundary()
        {
            if (elementCache == null)
            {
                return true;
            }

            if (elementCache == firstElement || elementCache == lastElement)
            {
                return true;
            }

            return false;
        }
    }

}

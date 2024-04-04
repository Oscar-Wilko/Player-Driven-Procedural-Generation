using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CamController : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvas_group;
    public CanvasGroup wfc_group;
    public CanvasGroup map_group;
    [Space]
    public DrawCanvas draw_canvas;
    public WFCVisual wfc_canvas;
    [Space]
    public SpriteRenderer canvas_visual;
    public SpriteRenderer wfc_visual;
    public SpriteRenderer map_visual;
    [Space]
    public RectTransform focus;
    public RectTransform side_focus;
    private Camera cam;
    [Header("Tracking Values")]
    private bool can_move = false;
    private Vector2 mouse_pos = Vector2.zero;
    [Header("Tweaking Values")]
    public float cam_offset;
    public float cam_speed;
    public float cam_speed_mouse;
    public float scroll_factor;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        canvas_group.gameObject.SetActive(true);
        wfc_group.gameObject.SetActive(true);
        map_group.gameObject.SetActive(true);
        SetCamState(0);
    }

    private void Update()
    {
        if (CanMove())
        {
            CheckMove();
            CheckZoom();
        }
        mouse_pos = Input.mousePosition;
    }

    private bool CanMove()
    {
        bool mouse_over_ui = EventSystem.current.IsPointerOverGameObject();
        return can_move && !mouse_over_ui;
    }

    private void CheckMove()
    {
        Vector3 move_vec = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"),0);
        move_vec *= cam_speed * Time.deltaTime;
        Vector3 move_vec_mouse = new Vector3(mouse_pos.x - Input.mousePosition.x, mouse_pos.y - Input.mousePosition.y, 0);
        move_vec_mouse *= cam_speed_mouse;
        transform.position += move_vec;
        if (Input.GetMouseButton(0))
            transform.position += move_vec_mouse * cam.orthographicSize;
    }

    private void CheckZoom()
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - Input.mouseScrollDelta.y * scroll_factor, 0.1f, 20);
    }

    public void SetCamState(int state)
    {
        // Update cam variables
        cam.orthographicSize = 5;
        can_move = state == 2;
        // Update external components
        draw_canvas.enabled = state == 0;
        wfc_canvas.enabled = state == 1;
        canvas_visual.enabled = state != 2;
        wfc_visual.enabled = state != 0;
        map_visual.enabled = state == 2;
        switch (state)
        {
            case 0:
                canvas_visual.transform.position = FocalPoint(true);
                canvas_visual.transform.localScale = Vector3.one;
                break;
            case 1:
                canvas_visual.transform.position = FocalPoint(false);
                wfc_visual.transform.position = FocalPoint(true);
                canvas_visual.transform.localScale = Vector3.one * 0.25f;
                wfc_visual.transform.localScale = Vector3.one;
                break;
            case 2:
                wfc_visual.transform.position = FocalPoint(false);
                map_visual.transform.position = FocalPoint(true);
                wfc_visual.transform.localScale = Vector3.one * 0.25f;
                break;
        }
        // Update alpha, interactability, raycast blocking of UI elements
        UpdateCanvasGroup(canvas_group, state, 0);
        UpdateCanvasGroup(wfc_group, state, 1);
        UpdateCanvasGroup(map_group, state, 2);
    }

    private void UpdateCanvasGroup(CanvasGroup group, int state, int state_req)
    {
        group.alpha = state == state_req ? 1 : 0;
        group.interactable = state == state_req;
        group.blocksRaycasts = state == state_req;
    }

    private Vector3 FocalPoint(bool main_focus)
    {
        Vector3 point = Camera.main.ScreenToWorldPoint(main_focus ? RectMidPoint(focus) : RectMidPoint(side_focus));
        point.z = 0;
        return point;
    }

    public static Vector3 RectMidPoint(RectTransform rect)
    {
        Vector3 point = rect.position;
        point += new Vector3((0.5f - rect.pivot.x) * rect.sizeDelta.x, (0.5f - rect.pivot.y) * rect.sizeDelta.y);
        return point;
    }
}

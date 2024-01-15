using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamController : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup state_1_group;
    public CanvasGroup state_2_group;
    public DrawCanvas draw_canvas;
    private Camera cam;
    [Header("Tracking Values")]
    private bool can_move = false;
    [Header("Tweaking Values")]
    public float cam_offset;
    public float cam_scaling;
    public float cam_speed;
    public float scroll_factor;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        SetCamState(0);
    }

    private void Update()
    {
        if (can_move)
        {
            CheckMove();
            CheckZoom();
        }
    }

    private void CheckMove()
    {
        Vector3 move_vec = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"),0);
        move_vec *= cam_speed * Time.deltaTime;
        transform.position += move_vec;
    }

    private void CheckZoom()
    {
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - Input.mouseScrollDelta.y * scroll_factor, 1, 30);
    }

    public void SetCamState(int state)
    {
        // Update external components
        draw_canvas.active = state == 0;
        // Update alpha, interactability, raycast blocking of UI elements
        state_1_group.alpha = state == 0 ? 1 : 0;
        state_1_group.interactable = state == 0;
        state_1_group.blocksRaycasts = state == 0;
        state_2_group.alpha = state == 1 ? 1 : 0;
        state_2_group.interactable = state == 1;
        state_2_group.blocksRaycasts = state == 1;
        // Update cam variables
        transform.position = new Vector3(state == 0 ? 0 : cam_offset,0,-10);
        cam.orthographicSize = state == 1 ? cam_scaling : 5;
        can_move = state == 1;
    }
}

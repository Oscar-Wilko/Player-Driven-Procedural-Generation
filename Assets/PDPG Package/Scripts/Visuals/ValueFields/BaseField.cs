using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseField : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject fullSegment;
    [SerializeField] private GameObject viewToggleLeft;
    [SerializeField] private GameObject viewToggleRight;

    private bool viewState;
    private float initHeight;

    protected virtual void Awake()
    {
        initHeight = GetComponent<RectTransform>().sizeDelta.y;
    }

    protected virtual void Start()
    {
        ToggleView(false);
    }

    public void ToggleView(bool newState)
    {
        viewState = newState;
        viewToggleLeft.SetActive(newState);
        viewToggleRight.SetActive(!newState);
        fullSegment.SetActive(newState);
        GetComponent<RectTransform>().sizeDelta = new Vector2(0, newState ? initHeight : 50);
    }

    public void ToggleView() => ToggleView(!viewState);
}

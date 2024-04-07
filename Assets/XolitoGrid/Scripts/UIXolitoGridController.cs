using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Xolito.Utilities;

public class UIXolitoGridController : MonoBehaviour
{
    GridController grid;
    [SerializeField]
    Color enabledColor;
    [SerializeField]
    Color disabledColor;
    [SerializeField]
    Color inactivedColor;
    [SerializeField]
    UISprite SaveImage;
    [SerializeField]
    UISprite randomImage;
    [SerializeField]
    UISprite visibleImage;
    [SerializeField]
    UISprite layerImage;

    [Serializable]
    public struct UISprite
    {
        public Image image;
        public Image backGround;
    }

    private void Awake()
    {
         grid = FindObjectOfType<GridController>();
    }

    void Start()
    {
        //grid.OnDrawLinesEnabled += (value) => ChangeColor(value, );
        grid.OnRandomEnabled += (value) => ChangeColor(value, randomImage.backGround);
        grid.OnShowCursorEnabled += (value) => ChangeColor(value, visibleImage.backGround);

        ChangeColor(grid.Random, randomImage.backGround);
        ChangeColor(grid.ShowCursor, visibleImage.backGround);
        SaveImage.backGround.color = inactivedColor;
    }

    void Update()
    {
        
    }

    public void ChangeColor(bool value, Image image)
    {
        if (value)
            image.color = enabledColor;
        else
            image.color = disabledColor;
    }
}

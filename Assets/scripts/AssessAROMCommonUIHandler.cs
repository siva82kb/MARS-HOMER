using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.UI;
using System;
using UnityEditor;
using System.IO;


public class CommonUI : MonoBehaviour
{
    public LineRenderer trajectoryLineRenderer;
    public LineRenderer aromBoxLineRenderer;
    public LineRenderer aromBoxLineRendererOld;
    public LineRenderer aromLine1Renderer;
    public LineRenderer aromLine2Renderer;
    public LineRenderer aromLine1RendererOld;
    public LineRenderer aromLine2RendererOld;
    public GameObject aromAreaBox;
    public GameObject aromAreaBoxOld;
    public Text instructionText;
    public Text marsEPPosText;
    public Text xRangeValueText;
    public Text yRangeValueText;
    public Button recalibrateButton;
    public GameObject circlePrefab;
    public static readonly Color LIGHT_GRAY = new Color(0.65f, 0.65f, 0.65f);
    public static readonly Color LIGHTER_GRAY = new Color(0.93f, 0.93f, 0.93f);
    public static readonly Color DARK_RED = new Color(0.8f, 0f, 0f);
    public static readonly Color DARKER_RED = new Color(0.6f, 0f, 0f);
    public static readonly Color LIGHT_GREEN = new Color(0.5f, 1f, 0.5f);
    public static readonly Color LIGHTER_GREEN = new Color(0.75f, 1f, 0.75f);
    public static readonly Color LIGHT_BLUE = new Color(0.5f, 0.5f, 1f);
    public static readonly Color LIGHTER_BLUE = new Color(0.75f, 0.75f, 1f);
    public static readonly Color LIGHT_BROWN = new Color(0.76f, 0.60f, 0.42f);
    public static readonly Color LIGHTER_BROWN = new Color(0.86f, 0.70f, 0.52f);

    void Start()
    {
        // Initialize line renderers
        ClearLineRenderers();

        // Trajectory linerenderer settings
        trajectoryLineRenderer.startWidth = 0.025f;
        trajectoryLineRenderer.endWidth = 0.025f;
        // Light gray colored line.
        trajectoryLineRenderer.startColor = LIGHT_GRAY;
        trajectoryLineRenderer.endColor = LIGHT_GRAY;

        // AROM Box
        aromBoxLineRenderer.startWidth = 0.05f;
        aromBoxLineRenderer.endWidth = 0.05f;
        aromBoxLineRenderer.startColor = DARKER_RED;
        aromBoxLineRenderer.endColor = DARKER_RED;
        
        // AROM Lines
        aromLine1Renderer.startWidth = 0.05f;
        aromLine1Renderer.endWidth = 0.05f;
        aromLine1Renderer.startColor = DARKER_RED;
        aromLine1Renderer.endColor = DARKER_RED;
        aromLine2Renderer.startWidth = 0.05f;
        aromLine2Renderer.endWidth = 0.05f;
        aromLine2Renderer.startColor = DARKER_RED;
        aromLine2Renderer.endColor = DARKER_RED;
        
        // AROM Box (Old)
        aromBoxLineRendererOld.startWidth = 0.01f;
        aromBoxLineRendererOld.endWidth = 0.01f;
        aromBoxLineRendererOld.startColor = LIGHT_BLUE;
        aromBoxLineRendererOld.endColor = LIGHT_BLUE;
        
        // AROM Lines (Old)
        aromLine1RendererOld.startWidth = 0.01f;
        aromLine1RendererOld.endWidth = 0.01f;
        aromLine1RendererOld.startColor = LIGHT_BLUE;
        aromLine1RendererOld.endColor = LIGHT_BLUE;
        aromLine2RendererOld.startWidth = 0.01f;
        aromLine2RendererOld.endWidth = 0.01f;
        aromLine2RendererOld.startColor = LIGHT_BLUE;
        aromLine2RendererOld.endColor = LIGHT_BLUE;
        
        // Hide recalibrate button initially
        recalibrateButton.gameObject.SetActive(false);
    }

    public void ClearLineRenderers()
    {
        // Set counts to 0.
        trajectoryLineRenderer.positionCount = 0;
        aromBoxLineRenderer.positionCount = 0;
        aromBoxLineRendererOld.positionCount = 0;
        aromLine1Renderer.positionCount = 0;
        aromLine2Renderer.positionCount = 0;
        aromLine1RendererOld.positionCount = 0;
        aromLine2RendererOld.positionCount = 0;
        // Clearn the area boxes
        aromAreaBox.transform.position = Vector3.zero;
        aromAreaBox.transform.localScale = Vector3.zero;
        aromAreaBox.SetActive(false);
        Color boxColor = new Color(0f, 0f, 0f, 1.0f);
        aromAreaBox.GetComponent<Renderer>().material.color = boxColor;
        aromAreaBoxOld.transform.position = Vector3.zero;
        aromAreaBoxOld.transform.localScale = Vector3.zero;
        aromAreaBoxOld.SetActive(false);
        aromAreaBoxOld.GetComponent<Renderer>().material.color = boxColor;
    }

    public void EnableRecalibrateButton()
    {
        recalibrateButton.interactable = true;
        recalibrateButton.gameObject.SetActive(true);
    }
}
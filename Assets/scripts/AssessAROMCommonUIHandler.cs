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
    public LineRenderer rawAromBoxLineRenderer;
    public LineRenderer adjustedAromBoxLineRenderer;
    public LineRenderer rawAromBoxLineRendererOld;
    public LineRenderer adjustedAromBoxLineRendererOld;
    public LineRenderer rawAromLine1Renderer;
    public LineRenderer rawAromLine2Renderer;
    public LineRenderer rawAromLine1RendererOld;
    public LineRenderer rawAromLine2RendererOld;
    public LineRenderer adjustedAromLine1Renderer;
    public LineRenderer adjustedAromLine2Renderer;
    public LineRenderer adjustedAromLine1RendererOld;
    public LineRenderer adjustedAromLine2RendererOld;
    public GameObject aromAreaBox;
    public GameObject aromAreaBoxOld;
    public Text instructionText;
    public Text marsEPPosText;
    public Text xRangeValueText;
    public Text yRangeValueText;
    public Button recalibrateButton;
    public GameObject circlePrefab;
    public static readonly Color LIGHT_GRAY = new Color(0.83f, 0.83f, 0.83f);
    public static readonly Color LIGHTER_GRAY = new Color(0.93f, 0.93f, 0.93f);
    public static readonly Color DARK_RED = new Color(0.8f, 0f, 0f);
    public static readonly Color DARKER_RED = new Color(0.6f, 0.75f, 0.75f);
    public static readonly Color LIGHT_GREEN = new Color(0.5f, 1f, 0.5f);
    public static readonly Color LIGHTER_GREEN = new Color(0.75f, 1f, 0.75f);
    public static readonly Color LIGHT_BLUE = new Color(0.5f, 0.5f, 1f);
    public static readonly Color LIGHTER_BLUE = new Color(0.75f, 0.75f, 1f);
    public static readonly Color LIGHT_BROWN = new Color(0.76f, 0.60f, 0.42f);
    public static readonly Color LIGHTER_BROWN = new Color(0.86f, 0.70f, 0.52f);

    void Start()
    {
        // Initialize line renderers
        trajectoryLineRenderer.positionCount = 0;
        rawAromBoxLineRenderer.positionCount = 0;
        adjustedAromBoxLineRenderer.positionCount = 0;
        rawAromBoxLineRendererOld.positionCount = 0;
        adjustedAromBoxLineRendererOld.positionCount = 0;
        rawAromLine1Renderer.positionCount = 0;
        rawAromLine2Renderer.positionCount = 0;
        adjustedAromLine1Renderer.positionCount = 0;
        adjustedAromLine2Renderer.positionCount = 0;
        rawAromLine1RendererOld.positionCount = 0;
        rawAromLine2RendererOld.positionCount = 0;
        adjustedAromLine1RendererOld.positionCount = 0;
        adjustedAromLine2RendererOld.positionCount = 0;

        // Trajectory linerenderer settings
        trajectoryLineRenderer.startWidth = 0.05f;
        trajectoryLineRenderer.endWidth = 0.05f;
        // Light gray colored line.
        trajectoryLineRenderer.startColor = LIGHT_GRAY;
        trajectoryLineRenderer.endColor = LIGHT_GRAY;

        // AROM Box
        rawAromBoxLineRenderer.startWidth = 0.05f;
        rawAromBoxLineRenderer.endWidth = 0.05f;
        rawAromBoxLineRenderer.startColor = DARKER_RED;
        rawAromBoxLineRenderer.endColor = DARKER_RED;
        adjustedAromBoxLineRenderer.startWidth = 0.1f;
        adjustedAromBoxLineRenderer.endWidth = 0.1f;
        adjustedAromBoxLineRenderer.startColor = DARK_RED;
        adjustedAromBoxLineRenderer.endColor = DARK_RED;

        // AROM Lines
        rawAromLine1Renderer.startWidth = 0.05f;
        rawAromLine1Renderer.endWidth = 0.05f;
        rawAromLine1Renderer.startColor = DARKER_RED;
        rawAromLine1Renderer.endColor = DARKER_RED;
        adjustedAromLine1Renderer.startWidth = 0.1f;
        adjustedAromLine1Renderer.endWidth = 0.1f;
        adjustedAromLine1Renderer.startColor = DARK_RED;
        adjustedAromLine1Renderer.endColor = DARK_RED;
        rawAromLine2Renderer.startWidth = 0.05f;
        rawAromLine2Renderer.endWidth = 0.05f;
        rawAromLine2Renderer.startColor = DARKER_RED;
        rawAromLine2Renderer.endColor = DARKER_RED;
        adjustedAromLine2Renderer.startWidth = 0.1f;
        adjustedAromLine2Renderer.endWidth = 0.1f;
        adjustedAromLine2Renderer.startColor = DARK_RED;
        adjustedAromLine2Renderer.endColor = DARK_RED;

        // AROM Box (Old)
        rawAromBoxLineRendererOld.startWidth = 0.01f;
        rawAromBoxLineRendererOld.endWidth = 0.01f;
        rawAromBoxLineRendererOld.startColor = LIGHTER_BLUE;
        rawAromBoxLineRendererOld.endColor = LIGHTER_BLUE;
        adjustedAromBoxLineRendererOld.startWidth = 0.05f;
        adjustedAromBoxLineRendererOld.endWidth = 0.05f;
        adjustedAromBoxLineRendererOld.startColor = LIGHT_BLUE;
        adjustedAromBoxLineRendererOld.endColor = LIGHT_BLUE;

        // AROM Lines (Old)
        rawAromLine1RendererOld.startWidth = 0.01f;
        rawAromLine1RendererOld.endWidth = 0.01f;
        rawAromLine1RendererOld.startColor = LIGHTER_BLUE;
        rawAromLine1RendererOld.endColor = LIGHTER_BLUE;
        adjustedAromLine1RendererOld.startWidth = 0.05f;
        adjustedAromLine1RendererOld.endWidth = 0.05f;
        adjustedAromLine1RendererOld.startColor = LIGHT_BLUE;
        adjustedAromLine1RendererOld.endColor = LIGHT_BLUE;
        rawAromLine2RendererOld.startWidth = 0.01f;
        rawAromLine2RendererOld.endWidth = 0.01f;
        rawAromLine2RendererOld.startColor = LIGHTER_BLUE;
        rawAromLine2RendererOld.endColor = LIGHTER_BLUE;
        adjustedAromLine2RendererOld.startWidth = 0.05f;
        adjustedAromLine2RendererOld.endWidth = 0.05f;
        adjustedAromLine2RendererOld.startColor = LIGHT_BLUE;
        adjustedAromLine2RendererOld.endColor = LIGHT_BLUE;

        // Hide recalibrate button initially
        recalibrateButton.gameObject.SetActive(false);
    }
}
﻿using Academy.HoloToolkit.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class TransformManager : Singleton<TransformManager>
{
    float expandAnimationCompletionTime;
    // Store a bool for whether our Scan model is expanded or not.
    bool isModelExpanding = false;
    public bool isScaling = false;
    public bool isSlicing = false;
    public bool isMoving = false;
    public bool isRotating = false;

    // KeywordRecognizer object.
    public KeywordRecognizer keywordRecognizer;

    // Defines which function to call when a keyword is recognized.
    delegate void KeywordAction(PhraseRecognizedEventArgs args);
    Dictionary<string, KeywordAction> keywordCollection;

    void Start()
    {
        keywordCollection = new Dictionary<string, KeywordAction>();

        // Add keyword to start manipulation.
        keywordCollection.Add("Move Scan", MoveScanCommand);
        
        keywordCollection.Add("Scale Scan", ScaleScanCommand);
        
        keywordCollection.Add("Slice Scan", SliceScanCommand);

        keywordCollection.Add("Rotate Scan", RotateScanCommand);

        keywordCollection.Add("Move Note", MoveNoteCommand);

        // 5.a: Add keyword Reset Model to call the ResetScanCommand function.
        keywordCollection.Add("Reset Scan", ResetScanCommand);

        // Initialize KeywordRecognizer with the previously added keywords.
        keywordRecognizer = new KeywordRecognizer(keywordCollection.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    void OnDestroy()
    {
        keywordRecognizer.Dispose();
    }

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        KeywordAction keywordAction;

        if (keywordCollection.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke(args);
        }
    }

    private void MoveScanCommand(PhraseRecognizedEventArgs args)
    {
        isMoving = true;
        isScaling = false;
        isSlicing = false;
        isRotating = false;
        GestureManager.Instance.Transition(GestureManager.Instance.ManipulationRecognizer);
    }

    private void ScaleScanCommand(PhraseRecognizedEventArgs args)
    {
        isMoving = false;
        isScaling = true;
        isSlicing = false;
        isRotating = false;
        GestureManager.Instance.Transition(GestureManager.Instance.ManipulationRecognizer);
    }

    private void SliceScanCommand(PhraseRecognizedEventArgs args)
    {
        isMoving = false;
        isSlicing = true;
        isScaling = false;
        isRotating = false;
        GestureManager.Instance.Transition(GestureManager.Instance.ManipulationRecognizer);
    }

    private void MoveNoteCommand(PhraseRecognizedEventArgs args)
    {
        isMoving = false;
        isSlicing = false;
        isScaling = false;
        isRotating = false;
        GestureManager.Instance.Transition(GestureManager.Instance.ManipulationRecognizer);
    }

    private void RotateScanCommand(PhraseRecognizedEventArgs args)
    {
        isMoving = false;
        isSlicing = false;
        isScaling = false;
        isRotating = true;
        GestureManager.Instance.Transition(GestureManager.Instance.ManipulationRecognizer);
    }

    private void ResetScanCommand(PhraseRecognizedEventArgs args)
    {
        // Reset local variables.
        isModelExpanding = false;

        // Disable the expanded model.
        ExpandModel.Instance.ExpandedModel.SetActive(false);

        // Enable the idle model.
        ExpandModel.Instance.gameObject.SetActive(true);

        // Enable the animators for the next time the model is expanded.
        Animator[] expandedAnimators = ExpandModel.Instance.ExpandedModel.GetComponentsInChildren<Animator>();
        foreach (Animator animator in expandedAnimators)
        {
            animator.enabled = true;
        }

        ExpandModel.Instance.Reset();
    }

    /*private void ExpandScanCommand(PhraseRecognizedEventArgs args)
    {
        // Swap out the current model for the expanded model.
        GameObject currentModel = ExpandModel.Instance.gameObject;

        ExpandModel.Instance.ExpandedModel.transform.position = currentModel.transform.position;
        ExpandModel.Instance.ExpandedModel.transform.rotation = currentModel.transform.rotation;
        ExpandModel.Instance.ExpandedModel.transform.localScale = currentModel.transform.localScale;

        currentModel.SetActive(false);
        ExpandModel.Instance.ExpandedModel.SetActive(true);

        // Play animation.  Ensure the Loop Time check box is disabled in the inspector for this animation to play it once.
        Animator[] expandedAnimators = ExpandModel.Instance.ExpandedModel.GetComponentsInChildren<Animator>();
        // Set local variables for disabling the animation.
        if (expandedAnimators.Length > 0)
        {
            expandAnimationCompletionTime = Time.realtimeSinceStartup + expandedAnimators[0].runtimeAnimatorController.animationClips[0].length * 0.9f;
        }

        // Set the expand model flag.
        isModelExpanding = true;

        ExpandModel.Instance.Expand();
    }*/

    public void Update()
    {
        if (isModelExpanding && Time.realtimeSinceStartup >= expandAnimationCompletionTime)
        {
            isModelExpanding = false;

            Animator[] expandedAnimators = ExpandModel.Instance.ExpandedModel.GetComponentsInChildren<Animator>();

            foreach (Animator animator in expandedAnimators)
            {
                animator.enabled = false;
            }
        }
    }
}
using UnityEditor;
using UnityEngine;

// Automatically enters Play Mode after the scripts compile, so you can just open
// the project and see the game. Delete this file if you would rather press Play
// yourself. It does nothing in batch mode (command-line builds / tests).
[InitializeOnLoad]
public static class AutoPlay
{
    static AutoPlay()
    {
        if (Application.isBatchMode)
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = true;
            }
        };
    }
}

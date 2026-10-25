using System;
using System.IO;
using DwSecretNotes.Platform;
using UnityEditor;
using UnityEngine;

namespace DwSecretNotes.EditorTools
{
    [InitializeOnLoad]
    static class EditorImagePickerRegistration
    {
        static EditorImagePickerRegistration() => EditorImagePickerHook.Register(PickImage);

        static void PickImage(Action<byte[]> onPicked)
        {
            try
            {
                var path = EditorUtility.OpenFilePanel("Image", "", "png,jpg,jpeg");
                onPicked(string.IsNullOrEmpty(path) ? null : ImageCodec.PrepareForUpload(File.ReadAllBytes(path)));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                onPicked(null);
            }
        }
    }
}

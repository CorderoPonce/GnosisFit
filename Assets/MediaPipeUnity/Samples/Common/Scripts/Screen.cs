// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity
{
  public class Screen : MonoBehaviour
  {
    [SerializeField] private RawImage _screen;

    private ImageSource _imageSource;

    public Texture texture
    {
      get => _screen.texture;
      set => _screen.texture = value;
    }

    public UnityEngine.Rect uvRect
    {
      set => _screen.uvRect = value;
    }

    public void Initialize(ImageSource imageSource)
    {
      _imageSource = imageSource;

#if UNITY_ANDROID && !UNITY_EDITOR
      // Android: configurar tamaño basado en pantalla, con rotación
      var rotation = _imageSource.rotation.Reverse();
      var euler = rotation.GetEulerAngles();
      bool isRotated90 = Mathf.Approximately(Mathf.Abs(euler.z), 90) || Mathf.Approximately(Mathf.Abs(euler.z), 270);

      float screenW = UnityEngine.Screen.width;
      float screenH = UnityEngine.Screen.height;

      // Centrar el RawImage
      _screen.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
      _screen.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
      _screen.rectTransform.pivot = new Vector2(0.5f, 0.5f);
      _screen.rectTransform.anchoredPosition = Vector2.zero;

      if (isRotated90)
      {
        // Pre-swap: poner (alto, ancho) para que después de rotar 90° se vea (ancho, alto)
        _screen.rectTransform.sizeDelta = new Vector2(screenH, screenW);
      }
      else
      {
        _screen.rectTransform.sizeDelta = new Vector2(screenW, screenH);
      }

      // Aplicar rotación
      Rotate(rotation);
      // Estirar los contenedores PADRES (sin tocar el RawImage)
      StretchAllParents();
#else
      Resize(_imageSource.textureWidth, _imageSource.textureHeight);
      Rotate(_imageSource.rotation.Reverse());
#endif
      ResetUvRect(RunningMode.Async);
      texture = imageSource.GetCurrentTexture();
    }

    public void Resize(int width, int height)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
      // En Android: no hacer nada aquí — Initialize() maneja todo
      return;
#else
      _screen.rectTransform.sizeDelta = new Vector2(width, height);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void StretchAllParents()
    {
      var canvas = _screen.GetComponentInParent<Canvas>();
      // Empezar desde el PADRE del RawImage (no tocar el RawImage mismo)
      Transform current = _screen.transform.parent;
      while (current != null && (canvas == null || current != canvas.transform))
      {
        var rt = current as RectTransform;
        if (rt != null)
        {
          rt.anchorMin = Vector2.zero;
          rt.anchorMax = Vector2.one;
          rt.offsetMin = Vector2.zero;
          rt.offsetMax = Vector2.zero;
          rt.localEulerAngles = Vector3.zero;
          rt.localScale = Vector3.one;
        }
        current = current.parent;
      }
      if (canvas != null)
      {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;
      }
    }
#endif

    public void Rotate(RotationAngle rotationAngle)
    {
      _screen.rectTransform.localEulerAngles = rotationAngle.GetEulerAngles();
    }

    public void ReadSync(Experimental.TextureFrame textureFrame)
    {
      if (!(texture is Texture2D))
      {
        texture = new Texture2D(_imageSource.textureWidth, _imageSource.textureHeight, TextureFormat.RGBA32, false);
        ResetUvRect(RunningMode.Sync);
      }
      textureFrame.CopyTexture(texture);
    }

    private void ResetUvRect(RunningMode runningMode)
    {
      var rect = new UnityEngine.Rect(0, 0, 1, 1);

      if (_imageSource.isVerticallyFlipped && runningMode == RunningMode.Async)
      {
        // In Async mode, we don't need to flip the screen vertically since the image will be copied on CPU.
        rect = FlipVertically(rect);
      }

      if (_imageSource.isFrontFacing)
      {
        // Flip the image (not the screen) horizontally.
        // It should be taken into account that the image will be rotated later.
        var rotation = _imageSource.rotation;

        if (rotation == RotationAngle.Rotation0 || rotation == RotationAngle.Rotation180)
        {
          rect = FlipHorizontally(rect);
        }
        else
        {
          rect = FlipVertically(rect);
        }
      }

      uvRect = rect;
    }

    private UnityEngine.Rect FlipHorizontally(UnityEngine.Rect rect)
    {
      return new UnityEngine.Rect(1 - rect.x, rect.y, -rect.width, rect.height);
    }

    private UnityEngine.Rect FlipVertically(UnityEngine.Rect rect)
    {
      return new UnityEngine.Rect(rect.x, 1 - rect.y, rect.width, -rect.height);
    }
  }
}

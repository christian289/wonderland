using System.Windows;
using Wonderland.UI.Controls;

namespace Wonderland.WpfApp.Services.Editing;

/// <summary>
/// 이동 동작
/// </summary>
public sealed class MoveAction(ParallaxCanvas canvas, Guid layerId, double oldX, double oldY) : IEditAction
{
    public void Undo() => canvas.UpdateLayerPosition(layerId, oldX, oldY);
}

/// <summary>
/// 크기 조절 동작
/// </summary>
public sealed class ResizeAction(ParallaxCanvas canvas, Guid layerId, Rect oldBounds) : IEditAction
{
    public void Undo() => canvas.UpdateLayerTransform(layerId, oldBounds.X, oldBounds.Y, oldBounds.Width, oldBounds.Height);
}

/// <summary>
/// 회전 동작
/// </summary>
public sealed class RotateAction(ParallaxCanvas canvas, Guid layerId, double oldRotation) : IEditAction
{
    public void Undo() => canvas.UpdateLayerRotation(layerId, oldRotation);
}

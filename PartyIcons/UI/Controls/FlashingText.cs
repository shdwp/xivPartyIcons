using System.Diagnostics;
using System.Numerics;
using ImGuiNET;

namespace PartyIcons.UI.Controls;

public class FlashingText
{
    public FlashingText()
    {
        _flashColor0 = new Vector4(0.4f, 0.1f, 0.1f, 1.0f);
        _flashColor1 = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
    }

    public bool IsFlashing
    {
        get => _isFlashing;
        set
        {
            if (_isFlashing != value)
            {
                _isFlashing = value;

                if (value)
                {
                    _stopwatch.Start();
                }
                else
                {
                    _stopwatch.Stop();
                }
            }
        }
    }

    public void Draw(string text)
    {
        Vector4 flashColor = _flashColor0;

        if (IsFlashing)
        {
            if (_stopwatch.ElapsedMilliseconds < 500)
            {
                flashColor = _flashColor1;
            }

            if (_stopwatch.ElapsedMilliseconds > 1000)
            {
                _stopwatch.Restart();
            }
        }

        if (IsFlashing)
        {
            ImGui.PushStyleColor(0, flashColor);
        }
        
        ImGui.Text(text);

        if (IsFlashing)
        {
            ImGui.PopStyleColor();
        }
    }
    
    private readonly Vector4 _flashColor0;
    private readonly Vector4 _flashColor1;

    private readonly Stopwatch _stopwatch = new();
    private bool _isFlashing;
}
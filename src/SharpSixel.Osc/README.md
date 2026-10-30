# SharpSixel.Osc

Terminal control through Operating System Commands (OSC) and cursor style sequences.
Depends only on `SharpSixel` (for `SixelColor`). Targets .NET Standard 2.0 and .NET 10.

```csharp
using SharpSixel;
using SharpSixel.Osc;

TerminalControl.SetTitle("My tool");
TerminalControl.SetForegroundColor("#FFD700");                  // #RGB or #RRGGBB
TerminalControl.SetBackgroundColor(new SixelColor(16, 32, 64)); // alpha is ignored
TerminalControl.SetCursorColor("#FF4040");
TerminalControl.SetCursorStyle(CursorStyle.BarBlinking);

TerminalControl.Reset();   // title (Windows), colors and cursor style back to the defaults
```

Every method takes an optional `TextWriter` (default `Console.Out`).
The terminal must process VT sequences (enable them on Windows); unsupported sequences are ignored.

| Method | Sequence |
| --- | --- |
| `SetTitle` / `ResetTitle` | `OSC 0` (control characters are removed; the previous title is restored on Windows only) |
| `SetForegroundColor` / `ResetForegroundColor` | `OSC 10` / `OSC 110` |
| `SetBackgroundColor` / `ResetBackgroundColor` | `OSC 11` / `OSC 111` |
| `SetCursorColor` / `ResetCursorColor` | `OSC 12` / `OSC 112` |
| `SetCursorStyle` | `CSI Ps SP q` (`CursorStyle`: Default, Box, Underscore, Bar; blinking or static) |
| `Reset` | all resets above, cursor style `Default` |

Invalid color text (`SetForegroundColor("red")`) throws an `ArgumentException`.

MIT, Copyright (c) 2026 Alexander Feuster.

# License

## ApexionGame.Entities.Stats

MIT License

Copyright (c) 2026 Apexion Games

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

## Third-party notices

This package is a derivative work. The stat algorithms — observer graph maintenance, modifier
stacks, cycle rejection, change propagation — originate upstream and are used under the MIT
license.

### Trove Stats

Copyright (c) 2023 Philippe St-Amand — <https://github.com/PhilSA/Trove> — MIT License.

The original design and implementation of the stat system.

### EncosyTower

Copyright (c) 2026 Laicasaane — <https://github.com/laicasaane/EncosyTower> — MIT License.

`EncosyTower.Entities.Stats` (the Unity ECS port of Trove Stats that this package was ported from),
plus `EncosyTower.Core`, which this package depends on at runtime, and a snapshot of
`EncosyTower.SourceGen.Common`, vendored into `Plugins/SourceGenerator.ApexionGame/`.

Both projects are distributed under the MIT License, whose terms are identical to the text
reproduced above, substituting the respective copyright holder.

# Modular Tanks for KSP(1)

A modern procedural tank system with a focus on artistic modularity and high visual fidelity.

## General design and generation

Each _tank_ is composed of a _stack_ of _segments_. A _segment_ is a pre-made _model_ (mesh) with selectable textures. The appearance of a tank is governed by the selected _style set_, which dictates the segments (and, by extension, textures) used to construct the tank. Possible style sets include "Plain (Stockalike)", "Saturn", "Spheres", and "Clustered Pills".

A tank is broken up into three _sections_: the _nose_, the _core_, and the _mount_. The nose and mount are required, while the core is optional. The nose and mount each constitute a single segment, while the core contains multiple segments to make up for the length of the tank.

To facilitate the separation of 'tank' (as in a container for propellant) from 'structural element', each segment is composed of the _body_ and the _skin_, respectively. For example, a tank butt may be composed of the hemispherical bulkhead (body) and the supporting stringers/structures (skin).

The simplest nose or mount would be a tank butt with a cap, such that the inner tank geometry is obscured. More complex options include aerodynamic nose cones and thrust structures, respectively. A stylized set such as "Saturn" may include _e.g._ the S-IC 5-engine mount.

There are four types of segments, all of which must be provided by a style set. The types are:

- Nose
- Mount
- Core (normal)
- Core (bulkhead)

Collectively, the nose and the mount are the _caps_. Some models may be shared between both.

Each segment type may contain multiple _models_. The user directly selects the nose and mount as a stylistic choice. The core segments (normal and support) models are procedurally combined to generate the core section of the tank, but the user specifies their textures. The combination of all segments to form a tank is a _stack_.

Structurally, the nose and mount contain the top and bottom bulkheads of the tank, respectively. They are required for this reason. A normal core segment is a segment of tank wall, with the entirety of the height of the segment being propellant. Based on the propellant mixture of the tank, the correct number of bulkhead segments will be placed, at the correct positions based on mixture ratio. This computation ignores 'auxiliary' resources such as EC and resources that are only included in small amounts compared to the predominant propellants. If there are more than three predominant propellants, the generation will fall back to using normal segments only.

Optionally (_e.g._ for RO use), the user is allowed to select the type of bulkhead segment used. This will be linked to thee fuel capacity of the tank, such that a separate bulkhead will be less efficient than an integrated bulkhead. If this is not enabled, then the bulkhead segment type is for aesthetic purposes only. In either case, the bulkheads segregate the core into _propellant sections_, each of which correspond to a tank containing a single type of propellant.

The stack configuration is computed by the `SegmentStacker`, yielding a `SegmentStack` that most closely matches the player's inputs. The stacker operates on _normalized dimensions_, where the diameter of the tank is scaled to be 1 unit. Then, the space taken up by each segment model is encoded in a single _native aspect ratio_ value, or the ratio of the height to the diameter, and the term _height_ may be used interchangeably with 'aspect ratio'. The stacker executes the following algorithm:

1. Determine the _normalized height_ of the tank
2. Determine the _total core height_ = normalized height - (height of nose + mount)
3. Determine the number of bulkhead segments required, if any, and the corresponding _bulkhead height_
4. Determine the _height of each propellant section_, based on the fuel mixture ratio
5. For each propellant section:
   1. Construct an empty `CoreSubstack`
   2. Iterating through the available normal core models in order of largest to smallest height, continually add the largest model that will fit to the substack until the total height of the substack is nearest to the desired propellent section height
6. Sum the total height thus determined, over the substacks as well as bulkheads _(or should this be applied to caps too??)_
7. Determine the _stretch factor_ required to make the computed total core height equal to the requested height
8. Compare the stretch factor to the maximum allowable stretch factor of each segment, and determine the ones, if any, which exceed their thresholds
9. Set the stretch factors of these segments to their maximum, and distribute the remainder among the remaining segments
10. (The above is assumed to resolve any stretching problems. If not, then doing any better is likely too complex and left to a later date.)
11. A `SegmentStack` with the positions and stretching factors thus computed is written out.

## Sample configuration

```text
MTStyleSet
{
  name = Canvas
  Nose
  {
    Model
    {
      name = Tank Butt
      path = /path/to/mu
      nativeDiameter = 2.5 // required
      maxStretchFactor = 0.2  // required, +- 20%
      centerlineXZ = 0.0, 0.0  // optional, vector2 containing the Unity XZ coordinates, default from model bounding box
      matingSurfaceY = 0.0, 1.0 // optional, vector2 containing the Unity Y coordinates of the bottom and top, respectively, default from model bounding box
      Texture
      {
        // path to all textures
      }
      Texture {} // etc.
    }
    Model {} // etc.
  }
  Mount {} // etc.
  Core {} // etc.
  CoreBulkhead{} // etc.
}
```

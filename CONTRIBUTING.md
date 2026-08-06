# Thank you for considering contributing to this project!
## Before you proceed with pull request, please read and acknowledge the following:

### No AI.
Do not contribute code that was AI-generated or written with heavy AI-assistance. No exceptions. 
This plugin directly controls player actions in game and AI-generated code could cause player actions that are non-compliant with the FFXIV server.

### Keep generalized methods/functions out of rotation logic.
Adding new methods/functions should be done in their proper sections to allow for broader use across the plugin.
For example; A method for tracking party buffs would go in CustomRotation_OtherInfo to allow other rotations to take advantage of that code.

### Do not refactor code outside RebornRotations/ExtraRotations without extensive testing.
Plugin code outside of actual rotation logic effects all job and duty rotations, please avoid making broad changes to those systems without extensive testing.

### Rotation code change testing.
Changes to code in RebornRotations should be tested with the Stone Sky Sea trials at level 60, 70, 80, 90 and 100, as RebornRotations need to be functional and performant at all levels.

### Keep your additions in uniform with the rest of the code.
Building the solution after making changes should not add any Errors, Warnings, or Messages in your IDE. 
No major code style changes please.

## Most welcomed changes.
Please feel free to:
- Status ID corrections
- Additions to the various lists for Invuln, AOE, Pyretic, etc.
- Spelling corrections in UI

## First-time contributors.
When opening a pull request for the first time please note that you've read and acknowledged this document.

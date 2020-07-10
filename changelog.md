# Simple Slavery Changelog
## v1.1.1
* Fix issue with slaves joining the colony immediately when emancipated.

## v1.1.0
* Add alert for when slaves are in timeout following an escape attempt. Hopefully this will make it more clear that they're supposed to be there.
* Keep track of slaves previously controlled by the colony. This is used so only pawns currently or previously controlled by the colony can have their collar abilities used. Selling a slave will mark them as no longer being controlled by the colony.
* Slaves will no longer attempt to escape once broken.

## v1.0.5
* Fix some mods removing enslaved hediffs since they're considered bad.

## v1.0.4
* Fix prisoners and animals not showing up for trades.

## v1.0.3
* Actually fix collar buttons disappearing from slaves when escaping.

## v1.0.2
* Fix collar buttons disappearing when slaves are escaping?

## v1.0.1
* Fix not being able to use special collar abilities (i.e. electric collar).

## v1.0.0
* Add new tab that lists slaves, displays their willpower, and allows for changing emancipation/shackle state.
* Make slaves only count for half a pawn according to the storyteller (same as prisoners).
* Add mod settings to:
	* Change how much a slave is worth to the storyteller.
	* Adjust the rate at which slaves' willpower decreases.
	* Disable escapes entirely.
	* Show slaves in the colonist bar.
* Alert for escaping slaves now will point to the escaping slave.
* The "miserable slaves" alert will now list which slaves are miserable.
* Now makes use of HugsLib's news feature.
### Backend Changes
* Measure slaves' willpower with a float in the range [0f-1f] instead of [0f-100f]. Saves are compatible.
* Add system to update slave info after a mod update to ensure we don't break saves.
* Move general slave gizmos to a `ThingComp` instead of using a Harmony patch.

## v0.1.3
* Fix slaves not rejoining correctly after attempting to escape when Prison Labor is enabled. See [Prison Labor#137](https://github.com/Aviuz/PrisonLabor/issues/137) for more info.

## v0.1.2
* Fix pawns not becoming slaves when Prison Labor is enabled. See [Prison Labor#137](https://github.com/Aviuz/PrisonLabor/issues/137) for more info.

## v0.1.1
* Slaves can not longer emancipate themselves or adjust their own shackles.

## v0.1.0.0
* Pawns can now be enslaved without failure if downed.
* Fix not being able to emancipate or adjust shackles without having regular prisoners.

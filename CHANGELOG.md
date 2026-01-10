# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-01-10
### This is the first official release of MirrorVR.
- Adjusted MIT headers to align with 2025-2026 MirrorVR timeline.
- EOSTransport is now no longer included with MirrorVR, to shrink down the package size.
- Added easy way to make Mirror authenticators, MirrorVRAuthenticatorBase.cs.
- Added two pre-made autenticators (CloudAuth, APKHashAuth) into new Examples folder.
- Hid Dedicated Server fields in the MirrorVRManager inspector for the time being.
- Moved Demo folder from `Assets/MirrorVR/Demo` to `Assets/MirrorVR/Examples/Demo`.

## [0.3.0] - 2025-12-28
- Updated to EOSTransport v3 Beta 2.
- Updated EOSSDK from 1.17.1.3 to 1.18.1.2.
- Fixed "Resource 'string/eos_login_protocol_scheme' not found in AndroidManifest.xml" Gradle Build Error
- Updated MetaVoiceChat to v4.2. See new features [here](https://github.com/Metater/MetaVoiceChat/releases/tag/v4.2).
- Removed support for multiple cosmetics in the same slot.
- Made cosmetics be able to be toggled while offline.
- Added a basic demo.
- Plus a lot more.

## [0.2.1/0.2.1.1] - 2025-08-14
- Fixed performance issues
- Updated EOSSDK from 1.17.0-CL39599718 to 1.17.1.3-CL44532354.
- Added Profiler Markers to some heavy methods in MirrorVR.
- Finished Steam Login Support.

## [0.2.0] - 2025-08-09
- Added cosmetics system!
- Added Dedicated Server support, via KCP transport.
- Merged MirrorVRManager and NetworkManager into one script.
- Added Epic Games Account and Device ID login support.
- Added BASIC Steam login support. Documentation in progress.
- Updated MetaVoiceChat to v2.3. See new features [here](https://github.com/Metater/MetaVoiceChat/releases/tag/v2.3).
- Bug fixes
- Code optimizations

## [0.1.4] - 2025-07-22
- Added Host Migration Alpha!
- Updated MetaVoiceChat to v2.1. See new features [here](https://github.com/Metater/MetaVoiceChat/releases/tag/v2.1).
- Fixed a bug that wouldn't let you build with MirrorVR in the project.

## [0.1.3] - 2025-07-18
- First documented GitHub release.
- Moved `Assets/Mirror/Transports/EOSTransport` to `Assets/MirrorVR/Transports/EOSTransport`.
- Moved `Assets/MirrorVR/Third-Party/MetaVoiceChat` to `Assets/MirrorVR/Third-Party/Metater/MetaVoiceChat`.
- Moved `Assets/MirrorVR/Third-Party/Concentus.2.2.2` to `Assets/MirrorVR/Third-Party/Metater/MetaVoiceChat/Concentus.2.2.2`.
- Updated MetaVoiceChat to v2. See new features [here](https://github.com/Metater/MetaVoiceChat/releases/tag/v2).
- Added fully-working colors! (untested over the network)
- (hopefully) fixed Oculus login issues.

## [0.1.2] - 2025-03-17
- Added option to add attributes when creating a lobby
- Fixed some bugs from v0.1.1

## [0.1.1] - 2025-03-16
- Added an editor only toggle on MirrorVRManager to use Epic Portal login instead of Oculus.
- Finished JoinRandomLobby code.

## [0.1.0] - 2025-03-15
- First Early Access release!
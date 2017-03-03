# Neptunium

Neptunium (or Nep for short) is a reimagining of [Hanasu](https://github.com/Amrykid/Hanasu), a streaming radio player catered to Japanese and Korean music. Hanasu began as a proof of concept and a hobby project. Neptunium began as an attend to build Hanasu as a Windows 10 Universal application. Neptunium runs on Windows 10 PC, Windows 10 Mobile phones and Xbox One (via Windows 10).

## Features
- Background Audio Playback: Neptunium supports the background audio playback functionality provided by the Universal Windows Platform. That means it runs seamlessly on the platforms listed above.
- Handoff: Handoff allows you to begin playing a station on one device and get it started on another device. Nep will also stop the stream from the first device as well. This works via Cortana/Continued App Experiences (also known as Project "Rome"). This functionality is currently disabled in builds in the store because of a compiling bug.
- Car Mode (Windows 10 Mobile only): For those who like listening while on the road, Nep has a special car mode that can be activated via Bluetooth. In Car Mode, Nep can announce song names for you and even do it using the system provided Japanese voice.

## Radio Stations
I don't plan on supporting custom stations inside the application unfortunately. The part of the reason is because I wanted to limit the scope of metadata sources I need to support. The other (major) reason is because at the time, Nep couldn't read the bitrate, sample rate and channel count from streams. That means I had to hard code every stream in the application and manually test each and everyone one. Of course, with recent updates to UWPShoutcastMSS, this is no longer a limitation.

If you have a station that you want to be included, consider submitting a pull request [here](https://github.com/Amrykid/Neptunium-Stations). Please keep in mind that the theme here is Japanese or Korean music.

## About the name
Neptunium was intended to be a codename for the next version of Hanasu. Early commits referenced it as "Neptunium (Hanasu Alpha)". The name comes from the chemical element, [Neptunium](https://en.wikipedia.org/wiki/Neptunium). It is also a backhanded reference to the main character of the [Hyperdimension Neptunia series](https://en.wikipedia.org/wiki/Hyperdimension_Neptunia). More specifically, the nickname "nep", comes from it.

Also, I wasn't a fan of the name Hanasu. Hanasu is the Japanese verb for "to speak" which didn't make very much sense for a music application. At the same time, the verb for "listening", "Kiku", didn't have the same ring to it. In the end, I decided to go along with Neptunium as a name.

## Screenshots
*coming soon...*

## Download
You can find Neptunium in the Windows Store via the following link: https://www.microsoft.com/store/apps/{app_id} where "app_id" is "9nblggh1r9cq" without the quotes. The reason that I am not directly linking to it is because I have the store listing set as hidden and I don't want it to be indexed in search engines.

## For Developers
Here are some of the projects or libraries I've used when writing this:
- [Crystal](https://github.com/Amrykid/Hanasu): My personal application framework. Provides MVVM and other boilerplate functionality.
- [UWPShoutcastMSS](https://github.com/Amrykid/UWPShoutcastMSS): Used for streaming Shoutcast/IceCast stations. Fun-fact: UWPShoutcastMSS was forked from code in Nep and re-integrated as a public library.
- [Kimono](https://github.com/Amrykid/Kimono/): A small library that contains a few GUUI controls I'm using.
- @dotMorten's [WindowsStateTriggers](https://github.com/dotMorten/WindowsStateTriggers/): A library containing a variety of state triggers. Very useful for designing adaptive user interfaces.
- @danesparza's [iTunesSearch](https://github.com/danesparza/iTunesSearch): This library is used for retreiving metadata from the iTunes store.
- @avatar29A's [MusicBrainz](https://github.com/avatar29A/MusicBrainz): This library is used for retreiving metadata from MusicBrainz.
- @xyzzer's [WinRTXamlToolkit](https://github.com/xyzzer/WinRTXamlToolkit/): Contains many GUI controls used in Nep.
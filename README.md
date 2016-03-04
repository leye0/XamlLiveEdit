# XamlLiveEdit
Live edit Xamarin Forms Xaml

TODO

1. XML interpreter for a nicer view of xml and an auto-formatting.
- Find libraries that do some colored syntax and indentation

2. Communication protocol improvement: 

A. Either improve the TCP start stop logic. StartKey/StopKey as consts shared in Mobile / Editor app. Reinit connection everytime instead of keeping it alive?

- Depending on the starting code, return the proper switch-case of what to do.
- Needs to indicate how bytes to read.

B. Or use WebSocket

C. Pseudo web-server. (Send raw http request)

3. Make connection to server easier. Add a button to indicate where is the server, then the device pushes its coordinates to the server. Subscribers logic.

4. Add a diagnostics semi-transparent overlay indicating bugs in XAML.
- In the catch, take the message and display it in the overlaid text at bottom.

5. Ability to modify code from the device. Sync between device and sofware.
- Semi-transparent overlay of xaml and a right sidebar for widgets, added in the xaml or screen (two-touch)

6. Have a page containing a grid listing all pages. (Ability to sync several XAML files instead of just one file)
- Alternate between a full screen, an editor mode and a view listing all pages in a grid. Maybe a + new button.

7. Binding. Mocking of ViewModels. (Generation / computation on server-side)
- MAYBE: Ability to edit ViewModel code on the device. This could eventually simply be another text editor under the XAML code.

8. Inclusion of XLabs stuff. Ex: Geolocation.




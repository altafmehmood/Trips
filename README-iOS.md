# iOS App Setup Guide

This guide will help you create an iOS app wrapper for your travel itineraries using WKWebView.

## Prerequisites

- Mac computer running macOS
- Xcode (download free from Mac App Store)
- Apple Developer Account (optional for testing on device, required for App Store)

## Step 1: Create New Xcode Project

1. Open Xcode
2. Click "Create a new Xcode project"
3. Select **iOS** → **App**
4. Click **Next**

## Step 2: Configure Project

Enter the following details:

- **Product Name**: `Travel Itineraries` (or your preferred name)
- **Team**: Select your Apple ID (or None for simulator only)
- **Organization Identifier**: `com.yourname.travelitineraries`
- **Interface**: **SwiftUI**
- **Language**: **Swift**
- **Storage**: None
- Uncheck "Include Tests"

Click **Next**, choose a location, and click **Create**

## Step 3: Add HTML Files to Project

1. In Xcode, right-click on the project folder (blue icon at top of file navigator)
2. Select **Add Files to "Travel Itineraries"...**
3. Navigate to this repository folder
4. Select these files/folders:
   - `index.html`
   - `India/` folder
   - `Australia/` folder
   - `CLAUDE.md` (optional)
5. **Important**: Check "Copy items if needed"
6. **Important**: Check "Create folder references" (not "Create groups")
7. Click **Add**

## Step 4: Replace ContentView.swift

1. In Xcode's file navigator, find `ContentView.swift`
2. Click on it to open
3. Delete all the existing code
4. Copy and paste this code:

```swift
import SwiftUI
import WebKit

struct ContentView: View {
    var body: some View {
        WebView()
            .edgesIgnoringSafeArea(.all)
    }
}

struct WebView: UIViewRepresentable {
    func makeUIView(context: Context) -> WKWebView {
        let webView = WKWebView()
        webView.scrollView.contentInsetAdjustmentBehavior = .never

        // Load the local HTML file
        if let htmlPath = Bundle.main.path(forResource: "index", ofType: "html") {
            let htmlUrl = URL(fileURLWithPath: htmlPath)
            let request = URLRequest(url: htmlUrl)
            webView.loadFileURL(htmlUrl, allowingReadAccessTo: htmlUrl.deletingLastPathComponent())
        }

        return webView
    }

    func updateUIView(_ uiView: WKWebView, context: Context) {
        // No updates needed
    }
}

#Preview {
    ContentView()
}
```

5. Save the file (Cmd+S)

## Step 5: Update Info.plist (Optional - for external maps)

If you want to allow external map links to open:

1. In the file navigator, find `Info.plist`
2. Right-click in the editor area
3. Select **Add Row**
4. Type: `NSAppTransportSecurity`
5. Expand the disclosure triangle
6. Add row: `NSAllowsArbitraryLoads` → Set to `YES`

## Step 6: Build and Run

1. At the top of Xcode, select a simulator (e.g., "iPhone 15 Pro")
2. Click the **Play** button (▶) or press **Cmd+R**
3. Wait for the simulator to launch and app to install
4. Your travel itineraries should now display in the app!

## Step 7: Customize App Icon (Optional)

1. In the file navigator, click on **Assets** (folder icon)
2. Click on **AppIcon**
3. Drag and drop icon images for different sizes
4. Recommended sizes: 1024x1024 (App Store), 180x180 (iPhone)

You can use online tools like [AppIconMaker](https://appiconmaker.co/) to generate all sizes.

## Step 8: Test on Your iPhone (Optional)

1. Connect your iPhone to your Mac
2. In Xcode, select your iPhone from the device dropdown (top)
3. Click **Play** to build and install on your device
4. On your iPhone: Go to **Settings** → **General** → **VPN & Device Management**
5. Trust your developer certificate
6. Launch the app from your home screen

## Troubleshooting

### White Screen / Blank App
- Make sure HTML files were added with "Create folder references"
- Check that `index.html` is at the root level
- Open the Console in Xcode to see any error messages

### Maps Not Loading
- Maps require internet connection
- Check that you allowed network access in Info.plist
- Verify OpenStreetMap tiles are loading (check browser first)

### "No such file or directory"
- Ensure you checked "Copy items if needed" when adding files
- Rebuild project: **Product** → **Clean Build Folder** (Shift+Cmd+K)
- Then build again (Cmd+R)

### Cannot Run on Device
- You need an Apple Developer Account ($99/year)
- Or test only in Simulator (free)

## Next Steps

### Enhance the App

1. **Add App Launch Screen**: Create a custom launch screen in SwiftUI
2. **Handle Link Navigation**: Intercept external links and open in Safari
3. **Offline Support**: Ensure all resources work without internet
4. **Add Refresh**: Pull-to-refresh to reload content
5. **Dark Mode**: Ensure itineraries look good in dark mode

### Publish to App Store

1. Enroll in Apple Developer Program ($99/year)
2. Create app listing in App Store Connect
3. Add screenshots, description, privacy policy
4. Archive and upload build from Xcode
5. Submit for review

## Advanced Features (Future)

- Push notifications for flight reminders
- Calendar integration
- Wallet pass integration for boarding passes
- Real-time flight status updates
- Weather widget
- Expense tracking

## Need Help?

- [Apple's iOS Developer Documentation](https://developer.apple.com/documentation/)
- [SwiftUI Tutorials](https://developer.apple.com/tutorials/swiftui)
- [WKWebView Documentation](https://developer.apple.com/documentation/webkit/wkwebview)

## Estimated Time

- **First-time setup**: 30-45 minutes
- **With Xcode experience**: 15-20 minutes
- **Testing and tweaking**: 30-60 minutes

---

**Note**: This creates a simple wrapper app. For production App Store release, consider adding error handling, loading indicators, and proper navigation controls.

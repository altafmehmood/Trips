# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains a collection of travel itineraries (Plans) and travel blogs (Blogs) organized into separate sections. The project is deployed via GitHub Pages and serves as a portfolio of travel plans, trip recaps, and photography.

## Repository Structure

```
Trips/ (root)
├── index.html                    # Landing page - links to Plans and Blogs
├── styles.css                    # Centralized stylesheet for all pages
├── CLAUDE.md                     # This file
├── .gitignore
│
├── Plans/                        # Trip itineraries and schedules
│   ├── index.html               # Plans overview (upcoming/past)
│   ├── Australia/
│   │   ├── index.html           # Australia itinerary (self-contained with embedded CSS)
│   │   ├── australia_itinerary.csv
│   │   └── CLAUDE.md            # Australia-specific documentation
│   ├── India/
│   │   └── index.html           # India itinerary
│   └── UnitedKingdom/
│       ├── index.html           # UK itinerary overview
│       ├── edinburgh.html       # Edinburgh destination page
│       ├── glasgow.html         # Glasgow destination page
│       ├── lake-district.html   # Lake District destination page
│       ├── london-arrival.html  # London arrival destination page
│       └── london-return.html   # London return destination page
│
└── Blogs/                        # Travel blog posts and recaps
    ├── index.html               # Blog overview (all posts)
    ├── 2025/
    │   └── Australia/
    │       ├── index.html       # Australia blog overview
    │       ├── sydney-early.html
    │       ├── sydney-late.html
    │       ├── melbourne.html
    │       ├── tasmania.html
    │       ├── cradle.html
    │       ├── freycinet.html
    │       ├── hobart.html
    │       ├── strahan.html
    │       ├── westcoast.html
    │       └── Photos/          # 235 photos (246MB, WebP format)
    │           └── 2025-12-*/   # Organized by date
    └── 2026/
        └── UnitedKingdom/
            ├── index.html           # UK blog overview
            ├── london-arrival.html  # Day 1: London landmarks
            ├── lake-district.html   # Days 2-4: Hiking & nature
            ├── glasgow.html         # Day 5: Scottish culture
            ├── edinburgh.html       # Days 6-7: History & hiking
            ├── london-return.html   # Days 8-9: Return & departure
            └── Photos/              # 104 photos (WebP, organized by date/location)
                └── 2026-01-*/       # Organized by date
```

## Architecture

### Centralized vs Embedded CSS

**Centralized Stylesheet (`styles.css`)**
- Root-level stylesheet used by most pages
- Contains all common components: cards, navigation, typography, colors
- Blog pages and overview pages reference: `<link rel="stylesheet" href="../../../styles.css">`
- Plans overview references: `<link rel="stylesheet" href="../styles.css">`

**Embedded CSS (Plans only)**
- Individual plan pages (e.g., `Plans/Australia/index.html`) retain embedded CSS
- These are self-contained itinerary documents with Leaflet.js maps
- Allows plans to be standalone documents with all styling inline

### Plans vs Blogs Organization

**Plans Folder**
- Contains trip itineraries organized by destination
- Each plan is forward-looking: schedules, activities, maps, logistics
- Plans for completed trips link to their corresponding blog posts
- Organized into "Upcoming" and "Past" sections on `Plans/index.html`

**Blogs Folder**
- Contains trip recaps organized by year and destination
- Each blog is retrospective: stories, photos, highlights, statistics
- Blog posts link back to original plans for reference
- Organized chronologically on `Blogs/index.html`

### Cross-Linking System

**From Plans to Blogs:**
- Completed trips show a banner: "✨ This trip is complete! Read the blog post..."
- Banner appears below back link, above main content
- Links to corresponding blog post

**From Blogs to Plans:**
- Blog posts include: "📅 View the original itinerary to see what we planned..."
- Reference appears below back link, above main content
- Links back to corresponding plan

### Static Site Deployment

- Deployed via GitHub Pages
- No build process or server-side logic required
- Main branch serves as deployment source
- Root `index.html` serves as simple landing page
- Photos stored directly in repository (246MB currently)

## Design System

### Color Palette
```css
--primary: #6366f1;        /* Indigo */
--secondary: #06b6d4;       /* Cyan */
--accent: #f59e0b;          /* Amber */
--success: #10b981;         /* Green */
--gray-50 through --gray-900
```

### Typography
- **Primary**: Space Grotesk (300-700 weights)
- Used for all text: body, headings, navigation, and UI elements
- Imported from Google Fonts
- Modern, geometric sans-serif with excellent readability

### Component System

**Cards** (`.trip-card`, `.nav-card`, `.blog-post-card`)
- Consistent padding, shadows, hover effects
- Color-coded top borders by destination
- Status badges for upcoming/completed trips

**Navigation** (`.back-link`)
- Always positioned top-left
- Color transitions on hover
- Points to parent section

**Banners** (`.blog-available-banner`, `.plan-reference`)
- Gradient backgrounds
- White text with underlined links
- Positioned between back link and main content

### Responsive Design
- Mobile-first approach
- Breakpoints: 768px (mobile), 1024px (tablet)
- Grid layouts collapse to single column on mobile
- Lightbox photo galleries adapt to viewport

### Interactive Maps (Plans Only)

**Map Library**
- Leaflet.js v1.9.4 (CDN)
- CARTO Voyager basemap tiles (no API keys)

**Map Types**
1. Flight Route Maps - Pacific-centered, curved polylines, emoji markers (🛫🛬)
2. Location Maps - Daily POIs with emoji markers (🏨🍽️🏖️🥾)

**Features**
- Custom `L.divIcon()` with emoji instead of default pins
- Interactive controls (zoom, drag, scroll)
- Responsive sizing (min-height: 375px)
- Hidden in print view with placeholder text

### Photo Galleries (Blogs Only)

**Gallery Grid** (`.photo-gallery`)
- Auto-fit grid: `minmax(300px, 1fr)`
- Photo cards with hover effects
- Clickable to open lightbox

**Lightbox Modal** (`.lightbox`)
- Full-screen overlay (rgba(0,0,0,0.95))
- Navigation buttons (prev/next)
- Photo metadata panel (location, camera, settings)
- Keyboard navigation (arrows, ESC)
- Mobile-optimized layout

### Print Styles

All pages include `@media print` rules:
- Hide navigation, banners, interactive elements
- Optimize typography for paper
- Page break controls
- Maps replaced with "📍 Interactive map available online"

## Development Commands

### Local Development
```bash
# Serve locally (choose one):
python -m http.server
python3 -m http.server 8000

# Or use any static file server
# Simply open index.html in browser for quick testing
```

### Git Workflow
- Primary branch: `main`
- Changes to main automatically deploy via GitHub Pages
- Commit frequently with descriptive messages
- Review changes with `git status` before committing

### Photo Management
- Photos stored in `Blogs/[Year]/[Destination]/Photos/`
- Organized by date folders (YYYY-MM-DD format)
- WebP format for optimal file size
- Currently tracked in git (may move to Git LFS or CDN in future)

## Adding New Content

### Adding a New Plan

1. Create folder: `Plans/[Destination]/`
2. Add `index.html` with itinerary details
3. Use embedded CSS or reference centralized stylesheet
4. Add to `Plans/index.html` in appropriate section (upcoming/past)
5. If using maps, follow Leaflet.js pattern from Australia

### Adding a New Blog Post

1. Create folder: `Blogs/[Year]/[Destination]/`
2. Add overview: `index.html` with trip stats and navigation
3. Add day pages: `[location]-[day].html` as needed
4. Create `Photos/` subfolder with date-organized images
5. All pages must reference: `<link rel="stylesheet" href="../../../styles.css">`
6. Add back links pointing to home or blog overview
7. Add plan reference banner linking to original itinerary
8. Add to `Blogs/index.html` with preview card

### Cross-Linking

When a trip is completed:
1. Add blog link to plan page (banner below back link)
2. Add plan reference to blog page (banner below back link)
3. Update `Plans/index.html` to show blog link in card footer
4. Update status badge from "Upcoming" to "Completed"

## Key Patterns

### Navigation Hierarchy
```
index.html (root)
├── Plans/index.html
│   └── Plans/[Destination]/index.html
└── Blogs/index.html
    └── Blogs/[Year]/[Destination]/index.html
        └── Blogs/[Year]/[Destination]/[day].html
```

### Stylesheet References
- Root pages: `<link rel="stylesheet" href="styles.css">`
- Plans overview: `<link rel="stylesheet" href="../styles.css">`
- Blog pages (3 levels deep): `<link rel="stylesheet" href="../../../styles.css">`
- Individual plan pages: Embedded CSS (self-contained)

### Card Color Themes
```css
.card-india, .card-plans: border-top: 4px solid #f59e0b (amber)
.card-australia, .card-blogs: border-top: 4px solid #6366f1 (indigo)
.card-uk: border-top: 4px solid #dc2626 (red)
```

## File Ignore Patterns

See `.gitignore` for current patterns:
- Development folders: `.vs/`, `.vscode/`, `Code/`
- Temporary files: `*-backup.html`, `*.tmp`
- Local settings: `.claude/settings.local.json`

Note: Photos are currently tracked in git but may be moved to external hosting in future.

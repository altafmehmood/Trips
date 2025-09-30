# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains a collection of travel itineraries, each organized as a separate subfolder with a self-contained HTML document. The project is deployed via GitHub Pages and serves as a portfolio of travel plans with interactive maps and detailed itineraries.

## Repository Structure

- `index.html` - Landing page with links to individual trip itineraries
- `Australia/` - 9-day Australia trip itinerary (Sydney, Melbourne, Tasmania)
  - Contains its own detailed CLAUDE.md with architecture specifics
- Each trip folder follows the same pattern: self-contained HTML with embedded CSS and JavaScript

## Architecture

### Multi-Trip Organization
Each trip is organized as a separate folder containing:
- `index.html` - Main itinerary file with embedded styling and JavaScript
- `CLAUDE.md` - Trip-specific development guidance
- All content is self-contained within each folder

### Static Site Deployment
- Deployed via GitHub Pages
- No build process or server-side logic required
- Main branch serves as deployment source
- Root `index.html` serves as landing page

### Shared Design System

All trip itineraries should follow this consistent design pattern:

#### Styling System
- Modern design system using CSS custom properties (CSS variables)
- Color palette:
  - Primary: `#6366f1` (indigo)
  - Secondary: `#06b6d4` (cyan)
  - Accent: `#f59e0b` (amber)
  - Gray scale: `--gray-50` through `--gray-900`
- Typography: Inter for body text, Poppins for headings (imported from Google Fonts)
- Shadow system: `--shadow-sm` through `--shadow-2xl` using defined CSS variables
- Border radius system: `--radius-sm` through `--radius-2xl`
- Activity categorization with semantic color coding

#### Layout & Responsive Design
- Responsive grid layout with mobile-first approach
- Max width: `1400px` for main container
- Semantic HTML5 structure
- CSS Grid and Flexbox for layouts
- Clamp() functions for fluid typography

#### Interactive Maps
- Leaflet.js integration via CDN (https://unpkg.com/leaflet@1.9.4/dist/leaflet.css)
- OpenStreetMap tiles (no API keys required)
- Custom markers with city-specific styling
- Maps centered on each location with appropriate zoom levels
- Interactivity can be disabled for presentation purposes

#### Print Functionality
- Comprehensive `@media print` rules
- Maps hidden in print view to focus on itinerary content
- Typography and layout optimized for printed documents
- Page break controls for better printouts

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
This is a Git repository using `main` as the primary branch. Standard git workflow applies:
- Commit changes with descriptive messages
- Changes to main branch automatically deploy via GitHub Pages
- Check git status before committing to review changes

## Adding New Trips

When adding a new trip itinerary:
1. Create a new folder with the trip name (e.g., `Japan/`)
2. Add an `index.html` file following the existing pattern
3. Update root `index.html` to include a link to the new trip
4. Consider adding a trip-specific CLAUDE.md if the architecture differs significantly

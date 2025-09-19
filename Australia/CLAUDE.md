# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains a single-file HTML travel itinerary for a 9-day Australia trip. The project is a self-contained HTML document with embedded CSS and JavaScript that displays an interactive travel itinerary with maps.

## File Structure

- `australia_google_maps_template.html` - Main HTML file containing the complete travel itinerary application
  - Embedded CSS with responsive design using CSS Grid and Flexbox
  - JavaScript for interactive Leaflet.js maps (Sydney, Melbourne, Tasmania)
  - Static map fallbacks with CSS-positioned markers
  - Color-coded activity categories and location markers

## Architecture

### HTML Structure
- Single-page application with semantic HTML5 structure
- Responsive grid layout for different screen sizes
- Three main city sections: Sydney, Melbourne, Tasmania
- Each city section contains itinerary details and interactive maps

### Styling System
- Custom CSS with earth-tone color palette (browns, greens, tans)
- Gradient backgrounds and backdrop blur effects
- Activity categorization with color coding:
  - Flight activities: Red (#B85450)
  - Harbour activities: Teal (#5B8A8A)
  - Culture activities: Brown (#8B7355)
  - Nature activities: Green (#65746B)
  - Food activities: Orange (#C4956C)
  - Beach activities: Blue-green (#7BA098)
  - Transport activities: Neutral brown (#9A8B7A)

### JavaScript Functionality
- Leaflet.js map integration for three regions
- Custom map markers with city-specific styling
- Static fallback positioning using CSS absolute positioning
- Map interactivity disabled (no zoom/drag) for presentation purposes

## Working with This Project

This is a static HTML file with no build process or dependencies beyond the Leaflet.js CDN. Changes can be made directly to the HTML file.

### Map Integration
- The project includes both JavaScript maps (Leaflet.js) and CSS fallback markers
- Google Maps API key placeholder exists but is not actively used
- Interactive maps are centered on each city with custom markers

### Responsive Design
- Grid layout adapts to different screen sizes
- Mobile-first approach with media queries at 1024px breakpoint
- Map sections stack vertically on smaller screens
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This repository contains a single-file HTML travel itinerary for a 9-day Australia trip. The project is a self-contained HTML document with embedded CSS and JavaScript that displays an interactive travel itinerary with maps.

## File Structure

- `index.html` - Main HTML file containing the complete travel itinerary application
  - Embedded CSS with modern design system using CSS custom properties
  - JavaScript for interactive Leaflet.js maps (Sydney, Melbourne, Tasmania)
  - Responsive design with print-friendly styling
  - Color-coded activity categories and location markers

## Architecture

### HTML Structure
- Single-page application with semantic HTML5 structure
- Responsive grid layout for different screen sizes
- Three main city sections: Sydney, Melbourne, Tasmania
- Each city section contains itinerary details and interactive maps

### Styling System
- Modern design system with CSS custom properties (CSS variables)
- Clean color palette using primary (#6366f1), secondary (#06b6d4), and accent colors
- Professional typography using Inter and Poppins font families
- Comprehensive print-friendly styling with `@media print` rules
- Activity categorization with semantic color coding
- Responsive grid layout with mobile-first approach

### JavaScript Functionality
- Leaflet.js map integration for three regions
- Custom map markers with city-specific styling
- Static fallback positioning using CSS absolute positioning
- Map interactivity disabled (no zoom/drag) for presentation purposes

## Working with This Project

This is a static HTML file with no build process or dependencies beyond the Leaflet.js CDN. Changes can be made directly to the HTML file.

### Development Commands
- No build process required - simply open `index.html` in a browser
- For local development, serve via any HTTP server (e.g., `python -m http.server` or Live Server extension)
- Project is deployed via GitHub Pages (main file is `index.html`)

### Map Integration
- Interactive Leaflet.js maps for Sydney, Melbourne, and Tasmania regions
- Maps are centered on each city with custom markers for activities
- No API keys required - uses OpenStreetMap tiles via Leaflet.js

### Print Functionality
- Comprehensive print styling with `@media print` rules
- Maps are hidden in print view to focus on itinerary content
- Typography and layout optimized for printed documents

### Responsive Design
- Mobile-first responsive design using CSS Grid and Flexbox
- Breakpoints accommodate various screen sizes
- Clean, professional appearance across all devices
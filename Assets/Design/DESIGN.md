# Game Design

This document specifies the design for the 'Event' page and its entry point on the 'HeroesPage'. The design follows the existing 'Canvas_Shop' structure and matches the fantasy dark aesthetic of the project.

## UI Design

### Color System
- **Surface Hierarchy:**
  - Base: Dark charcoal (#121212) with subtle vignette.
  - Panels: Deep metallic grey (#1E1E1E) with ornate borders.
  - Interactive: Hover states should add a subtle gold glow.
- **Intent Mapping:**
  - **Active/General (Green):** #4CAF50 - Used for 'Active' events and primary progress bars.
  - **Limited/Rare (Purple):** #9C27B0 - Used for 'Limited' time events and special highlights.
  - **Danger/Boss (Red):** #F44336 - Used for 'Boss' events and high-stakes challenges.
  - **Accent (Gold):** #D4AF37 - Used for headers, borders, and high-value rewards.

### Typography
- **Display/Headers:** Serif Font (e.g., 'Cinzel' or project equivalent), Bold, Gold-to-White vertical gradient. Large (48-64px).
- **Body/Description:** Sans-Serif (e.g., 'Inter' or project equivalent), Regular, Off-white (#E0E0E0). Standard (24-28px).
- **Technical/Timers:** Monospace or clean Sans-Serif, Light Gold (#FFD700), Small (18-20px).

### Layout: Canvas_Event (core)
Following the `Canvas_Shop` template:
- **Canvas_Event:** Render Mode 'Screen Space - Overlay', Sorting Order 45.
  - **SafeAreaRoot:** Anchors [0, 1], Size Delta [0, 0].
    - **Header (Top):** Height 200px. Contains ornate frame, "Events" title (centered), and a Back button (Top-Left).
    - **TabGroup (Below Header):** Height 120px. Horizontal Layout Group (Spacing: 20px). 
      - Tabs: "Active", "Limited", "Boss", "Rewards".
      - Unselected: Dark metallic background.
      - Selected: Glowing frame matching intent color (e.g., Green for Active).
    - **EventScroll (Center):** Fills remaining space. ScrollRect with Vertical Layout Group.
      - **EventPanel (Item):** Height 450px. 
        - **Background:** Event-specific thematic art (e.g., dark forest, crystal cave).
        - **Frame:** Uses `1.png` or `2.png` as an ornate overlay.
        - **Content Left:** Title, Description, Timer (with icon), Progress Bar (core for Forest/Active).
        - **Content Bottom:** "Rewards" label + row of item icons with counts.
        - **Action Button (Right):** Large "Enter" or "Challenge" button with a heavy glow matching the theme.

### Component: EventButton on HeroesPage (core)
- **Position:** Top-Right area of `HeroesPage` (adjacent to existing feature buttons).
- **Style:** Circular ornate frame (Gold).
- **Icon:** Event icon (calendar or trophy) with a "New" badge (red dot) if active events exist.
- **Label:** "Events" (Small, below icon).

## Gift Page Design

This section specifies the design for the 'Gift' page, ensuring consistency with the 'Shop' canvas and dark fantasy aesthetic.

### UI Design: Gift Page (core)
Following the `Canvas_Shop` template:
- **Canvas_Gift:** Render Mode 'Screen Space - Overlay', Sorting Order 50.
  - **SafeAreaRoot:** Anchors [0, 1], Size Delta [0, 0].
    - **Background:** Dark Forest Blue (#050810) with subtle vignette.
    - **TopBar (Header):** 
      - Ornate title frame with "Gifts" label.
      - **GiftCloseButton:** "X" button (Top-Right).
      - **Currencies:** Display Gems, Gold, and Water (same style as Main TopBar).
    - **Content (Scrollable/Centered):**
      - **Daily Gift Section:**
        - **Icon:** Ornate Chest.
        - **Text:** "Claim your daily rewards!"
        - **Rewards:** Coin (10,000) and Gem (50) icons.
        - **GiftDailyTimerText:** "Refreshes in: 04:32:18".
        - **GiftDailyClaimButton:** Large gold button with `GiftDailyClaimLabel`.
      - **Login Rewards Section:**
        - Header: "Login Rewards".
        - **Grid/Row:** 7 Reward Slots (Day 1 to Day 7).
        - Each Slot: `GiftLoginDayButton_{i}` with status label `GiftLoginStateText_{i}` (Done, Ready, Locked).
        - **GiftLoginTimerText:** "Resets in: 13d 04:32:18".
      - **Friend / Guild Gifts Section:**
        - Header: "Social Gifts".
        - **List:** Scrollable list of received gifts (up to 2 for prototype).
        - Each Entry: Sender Info, Reward Icon, and `GiftFriendClaimButton_{i}`.
        - **GiftClaimAllButton:** "Claim All" at the bottom of the list.
      - **Redeem Code Section:**
        - **RedeemInputGroup:** 
          - `GiftRedeemInput`: TMP InputField with "Enter Code..." placeholder.
          - `GiftRedeemButton`: "Redeem" action button.
        - **GiftRedeemStatusText:** Feedback message (e.g., "Invalid code", "Success!").

### Asset Design: Gift Page (core)
- **Visual Identity:** Consistent with `Canvas_Shop` and `Canvas_Event`.
- **Sprite Usage:**
  - **gifts/elements.png (core):** Sliced sprites for section frames, chest icons, and item slots.
  - **Main_UI/New (core):** Use standard currency icons and gold button slices.
  - **Reference Image (179278):** Match the layout and specific ornate gold detailing for the login slots and section headers.

### Game Feedback: Gift Page (core)
- **Genre Profile:** Tactical / Satisfaction-focused.
- **Interaction Map:**

| Interaction | Tier | Importance | Camera | Time | Transform | Visual | Audio | Rationale |
|------------|------|-----------|--------|------|-----------|--------|-------|-----------|
| Claim Button | core | Medium | — | 0.05s | Scale (1.1x) | Flash | Gold Ping | Acknowledge reward |
| Redeem Success| core | High | — | — | — | Confetti/Sparkle| Success Chime | Celebrate success |
| Input Focus | core | Minor | — | — | — | Border Glow | — | Clear interaction state|

### Component: GiftButton on HeroesPage (core)
- **Position:** Adjacent to the Events button on the `HeroesPage`.
- **Coordinates:** `RectSpec(800, 75, 100, 100)` (assuming Events is at 920, 75).
- **Style:** Circular ornate frame (Gold).
- **Icon:** Gift box icon.
- **Label:** "Gifts" (Small, below icon).
- **Badge:** Red dot for ready-to-claim gifts.

## Asset Design

### Visual Identity
- **Style:** High-fantasy dark theme. Ornate, heavy borders (metallic/gold).
- **Detail Level:** High texture density on frames; clean, readable typography.
- **Sprite Usage:**
  - **1.png (core):** Main ornate frame for Event Panels. Use as a sliced sprite (9-slicing) for variable widths.
  - **2.png (core):** Header/Title frame or decorative divider.
  - **Reference Image (75274):** Match the specific glow intensities and the way event panels overlap their backgrounds.

### Palette per Category
- **Environment/Banners:** Desaturated dark tones to ensure foreground readability.
- **UI Elements:** High-contrast gold/metallic against dark backgrounds.
- **Feedback Layers:** Highly saturated glows (Green, Purple, Red) to indicate interactivity and status.

## Game Feedback

- **Genre Profile:** High-Energy (Action/RPG).
- **Interaction Map:**

| Interaction | Tier | Importance | Camera | Time | Transform | Visual | Audio | Input | Rationale |
|------------|------|-----------|--------|------|-----------|--------|-------|-------|-----------|
| Open Event Page | core | Medium | — | — | Slide-in from Right | Fade-in Overlay | Light 'Whoosh' | — | Smooth transition |
| Tab Switch | core | Light | — | — | Subtle Scale (1.05x) | Glow shift | Soft Click | — | Acknowledge selection |
| Progress Bar Fill | optional | Minor | — | — | — | Particle sparks at tip | — | — | Celebrate progress |
| Challenge Button | core | Medium | — | — | Squash (0.9x) | Flash on press | Heavy Thud | 100ms buffer | Weighty interaction |

- **Assets needed:**
  - `EventPanel_Frame` (core): Sliced sprite from `1.png`.
  - `EventTab_Active` (core): Glowing variant of tab background.
  - `ProgressBar_Fill` (core): Glowing green texture.
  - `EventIcon_Default` (core): Trophy/Calendar icon for entry button.

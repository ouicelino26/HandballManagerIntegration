# Admin Design System

Generated: 2026-08-06  
Branch: feature/handwstat-admin-product-final-v1

---

## Design Philosophy

The WPF admin shell follows HandWStat's design language: dark green navigation sidebar with a clean off-white content area. Color tokens are drawn from HandWStat's CSS custom properties, adapted for WPF SolidColorBrush resources.

---

## Color Tokens (Themes/Colors.xaml)

### Content Area (Light Mode)
| Token | Value | Usage |
|---|---|---|
| AppBackgroundBrush | #F1EFE8 | Window/page background |
| SurfaceBrush | #FFFDF8 | Card backgrounds |
| ElevatedSurfaceBrush | #FFFFFF | Modal/dialog backgrounds |
| PrimaryTextBrush | #17231F | Primary text |
| SecondaryTextBrush | #52615B | Secondary/label text |
| MutedTextBrush | #78847F | Placeholder, disabled text |
| AccentBrush | #D7633C | Primary action color |
| SuccessBrush | #257352 | Success states |
| WarningBrush | #A86416 | Warning states |
| DangerBrush | #B5413D | Error/destructive states |
| InfoBrush | #2F6471 | Informational states |
| BorderBrush | #D8D9D1 | Borders, separators |
| FocusBrush | #EA8A55 | Focus rings |

### Navigation Shell (Dark Green)
| Token | Value | Usage |
|---|---|---|
| AdminShellBrush | #17372E | Nav sidebar background |
| AdminShellElevatedBrush | #21483D | Nav item hover |
| AdminShellTextBrush | #FFF8ED | Nav text |
| NavItemActiveBrush | #1E4D3F | Selected nav item |
| NavItemHoverBrush | #2A5A4A | Hovered nav item |
| NavItemTextBrush | #E8F0ED | Nav item text |
| NavItemActiveTextBrush | #FFFFFF | Active nav item text |

### Semantic Inputs
| Token | Value | Usage |
|---|---|---|
| InputBorderBrush | #C4C9C5 | TextBox, ComboBox border at rest |
| InputFocusBorderBrush | #EA8A55 | Input border when focused |
| ButtonPrimaryBrush | #D7633C | Primary button background |
| ButtonPrimaryHoverBrush | #C4552E | Primary button hover |
| ButtonDestructiveBrush | #B5413D | Destructive button fill |
| ButtonDestructiveHoverBrush | #9B342F | Destructive button hover |

---

## Spacing Tokens (Themes/Spacing.xaml)

| Token | Value (dp) |
|---|---|
| SpacingXS | 4 |
| SpacingS | 8 |
| SpacingM | 16 |
| SpacingL | 24 |
| SpacingXL | 32 |
| SpacingXXL | 48 |
| PagePadding | 24 |
| CardPadding | 16 |
| SectionGap | 24 |
| ItemGap | 8 |

---

## Typography Styles (Themes/Typography.xaml)

| Style Key | Size | Weight | Usage |
|---|---|---|---|
| PageTitleStyle | 24 | Bold | Page H1 |
| SectionHeaderStyle | 18 | SemiBold | Section headers |
| BodyTextStyle | 14 | Normal | Body copy |
| CaptionTextStyle | 12 | Normal | Captions, timestamps |
| LabelTextStyle | 13 | SemiBold | Form labels |
| CodeTextStyle | 13 | Normal (Consolas) | Code/IDs |
| ErrorTextStyle | 13 | Normal | Validation errors (DangerBrush) |

---

## Control Styles (Themes/Controls.xaml)

### Button Variants
| Style Key | Visual | Use Case |
|---|---|---|
| (implicit Button) | Transparent, no border | Base/icon-adjacent |
| PrimaryButtonStyle | AccentBrush fill, white text, 8px radius | Confirm, Save, Submit |
| SecondaryButtonStyle | Transparent + border, primary text | Cancel, Back |
| DestructiveButtonStyle | Red border at rest, red fill on hover | Delete, Archive |
| IconButtonStyle | Square, no border, hover bg | Toolbar icons |
| LoadingButtonStyle | Disabled state shows "..." text | In-flight operations |

---

## Form Styles (Themes/Forms.xaml)

- **AdminTextBoxStyle**: 8px padding, InputBorderBrush 1px, FocusBrush focus ring, transparent background
- **AdminComboBoxStyle**: height-matched to TextBox for alignment
- **AdminDatePickerStyle**: consistent with ComboBox appearance
- **AdminCheckBoxStyle**: 24px minimum hit target
- **AdminLabelStyle**: LabelText typography style applied

---

## Table Styles (Themes/Tables.xaml)

- **AdminDataGridStyle**: no outer border, flat header, row hover highlight
- **AdminDataGridColumnHeaderStyle**: light gray bg, semibold, bottom border only
- **AdminDataGridRowStyle**: alternating rows (SurfaceBrush / AppBackgroundBrush), hover AccentBrush 10%
- **AdminDataGridCellStyle**: 8/12 padding, no cell border

---

## Navigation Styles (Themes/Navigation.xaml)

- **NavItemStyle**: full-width ListBoxItem, dark green shell; selected=NavItemActiveBrush, hover=NavItemHoverBrush
- **NavGroupHeaderStyle**: small-caps, muted color, 8px top gap
- **NavBadgeStyle**: right-aligned rounded badge

---

## Dialog Styles (Themes/Dialogs.xaml)

- **AdminDialogStyle**: 400px min-width, ElevatedSurfaceBrush bg, 24px padding
- **AdminDialogTitleStyle**: SectionHeader typography
- **AdminDialogContentStyle**: Body text, 8px top/bottom margin
- **AdminDialogButtonRowStyle**: right-aligned flex row
- **AdminDialogCancelButtonStyle**: SecondaryButtonStyle alias
- **AdminDialogConfirmButtonStyle**: PrimaryButtonStyle alias
- **AdminConfirmationDialogStyle**: red confirm button (DestructiveButton) for destructive operations

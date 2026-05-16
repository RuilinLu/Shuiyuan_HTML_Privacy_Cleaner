# Third-Party Notices

This project embeds an offline anonymous identity pack for full-anonymous mode.
The pack is generated at build time from open-source libraries and local word
lists. The final Windows EXE does not contact these services or any network API
at runtime.

## DiceBear

- Project: https://github.com/dicebear/dicebear
- Site: https://www.dicebear.com/
- Use in this project: generates diverse SVG avatar artwork for anonymous users.
- License note: DiceBear core is MIT licensed. Individual avatar styles can have
  their own design licenses, so this project keeps this notice with the
  generated avatar pack and preserves style metadata in
  `assets/anonymous_identity_pack.metadata.json`.

## Faker

- Project: https://github.com/faker-js/faker
- Site: https://fakerjs.dev/
- Use in this project: generates fictional display names and usernames.
- License: MIT.

## No Real-Person Photo Dataset

The distributed anonymous identity pack intentionally uses open-source
illustrated/SVG avatars instead of real-person photo datasets. This avoids
introducing third-party portrait privacy, consent, and likeness risks into a
privacy-cleaning tool.

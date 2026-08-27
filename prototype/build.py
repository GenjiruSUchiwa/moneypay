#!/usr/bin/env python3
"""Assemble dist/index.html — fichier unique auto-suffisant (publication)."""
import pathlib
root = pathlib.Path(__file__).parent
html = (root / "index.html").read_text()
html = html.replace('<link rel="stylesheet" href="styles.css">',
                    "<style>\n" + (root / "styles.css").read_text() + "</style>")
html = html.replace('<script src="app.js"></script>',
                    "<script>\n" + (root / "app.js").read_text() + "</script>")
(root / "dist").mkdir(exist_ok=True)
(root / "dist" / "index.html").write_text(html)
print("dist/index.html :", len(html), "octets")

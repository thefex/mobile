# Mermaid Diagrams

To build [Mermaid](https://knsv.github.io/mermaid/) diagrams in real time,
in a terminal and from the repo root directory follow these steps:

```bash
# Only the first time
cd Docs/Diagram
npm install
cd ../..

cd Docs
node Diagrams/diagram-watcher.js
```

Put the Mermaid diagrams source code in `Doc/Diagrams` with `.mmd` extension.

In the Markdown documents in `Docs` add references to the diagrams as follows
(note the `.mmd.png` extension):

```markdown
![Unidirectional Architecture](Diagrams/unidirectional_architecture.mmd.png)
```

Whenever you modify a `.mmd` file, `diagram-watcher` will render the diagram.
Please note this can take a couple of seconds and that you'll need to refresh
the Markdown previewer to see the changes.

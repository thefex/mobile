var fs = require("fs");
var path = require("path");
var exec = require('child_process').exec;

fs.watch(".", { persistent: true, recursive: true }, function(ev, filename) {
    if (path.extname(filename) === ".mmd") {        
        var command = "node " + __dirname + "/node_modules/mermaid/bin/mermaid " + filename + " -o " + path.dirname(filename) + " -w 980";
        // console.log(command);
        console.log("Rendering " + filename + "...") 
        exec(command, function(error, stdout, stderr) {
            if (error || (error = stderr)) {
                console.log("Error: " + error);
                return;
            }
            console.log("Rendered!");
        });
    }
})

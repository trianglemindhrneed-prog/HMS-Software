// appointmentprintjs.js

console.log("Appointment print JS loaded");

// Print function
function printGridFromTable() {
    try {
        var table = document.getElementById("appointmentTable");
        if (!table) {
            alert("Table not found!");
            console.error("Table not found!");
            return;
        }

        // Clone the table so we don't modify the original
        var clone = table.cloneNode(true);

        // Remove all elements with 'no-print' class
        clone.querySelectorAll('.no-print').forEach(function (el) {
            el.remove();
        });

        // Open new window
        var w = window.open("", "_blank", "width=900,height=600");
        if (!w) {
            alert("Pop-up blocked! Allow pop-ups for this site.");
            return;
        }

        w.document.write('<html><head><title>Appointment Report</title>');
        w.document.write('<style>');
        w.document.write('table { width:100%; border-collapse: collapse; }');
        w.document.write('th, td { border:1px solid #000; padding:5px; text-align:left; }');
        w.document.write('th { background:#f0f0f0; }');
        w.document.write('</style>');
        w.document.write('</head><body>');
        w.document.write("<h2>Appointment Report</h2>");
        w.document.write(clone.outerHTML);
        w.document.write('</body></html>');

        w.document.close();
        w.focus();   // important for Chrome/Edge

        // Delay slightly to ensure rendering before printing
        setTimeout(function () {
            w.print();
            // w.close(); // optional
        }, 300);

        console.log("Print window opened successfully");
    } catch (err) {
        console.error("Error in printGridFromTable:", err);
        alert("Printing failed! Check console.");
    }
}

// Attach click handler once DOM is ready
document.addEventListener("DOMContentLoaded", function () {
    var btn = document.getElementById("btnPrint");
    if (btn) {
        btn.addEventListener("click", printGridFromTable);
        console.log("Print button attached");
    } else {
        console.error("Print button not found!");
    }
});

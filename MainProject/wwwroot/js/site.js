// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
let cb = document.getElementById("check"),
    cb2 = document.querySelectorAll(".checkbox2");

cb.addEventListener('change', function () {
    if (cb.checked == true) {
        for (let i = 0; i < cb2.length; i++) {
            cb2[i].checked = true;
        }
    }
    else {
        for (let i = 0; i < cb2.length; i++) {
            cb2[i].checked = false;
        }
    }
});
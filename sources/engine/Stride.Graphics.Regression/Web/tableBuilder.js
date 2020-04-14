/////////////////////////
// SOME CONSTANTS
/////////////////////////
var GOLD_SERVER = "\\\\stridebuild/GraphicsTestShare/";
var BUILD_FOLDER = "build/";
var USER_FOLDER = "user/";
var GOLD_FOLDER = "gold/";

/////////////////////////
// GLOBAL VARIABLES
/////////////////////////
var jsonData = null;
var nbFileToRead = 0;
var nbFileRead = 0;
var loadOnServer = true;

/////////////////////////
// CODE
/////////////////////////
function parseJsonText(fileText) {
    var data = $.parseJSON(this.result);
    ++nbFileRead;
    
    if (jsonData == null)
        jsonData = data;
    else
        jsonData = jsonData.concat(data);
    
    if (nbFileRead >= nbFileToRead) {
        extractJsonInformations();
    }
}

function handleFileSelect(evt) {
    nbFileToRead = evt.target.files.length;
    nbFileRead = 0;
    jsonData = null;
    
    for (var i = 0; i < nbFileToRead; ++i) 
    {
        var fileReader = new FileReader();
        fileReader.onload = parseJsonText;
        var file = evt.target.files[i];
        fileReader.readAsText(file);
    }
}

function updateFilter(selectBox) {
    if (selectBox == null || selectBox.selectedIndex == 0)
        displayTestResults(null, radioDisplaySuccess(), radioDisplayFail());
    else
        displayTestResults(selectBox.value, radioDisplaySuccess(), radioDisplayFail());
}

function radioChange(evt) {
    updateFilter(document.getElementById("allTests"));
}

function selectTest(evt) {
    updateFilter(evt.target);
}

function loadLocalChange(evt) {
    loadOnServer = !evt.target.checked;
    updateFilter(document.getElementById("allTests"));
}

function clearTable() {
    var table = document.getElementById("table");
    if (table != null) {
        // remove all the children except the text and the header
        var nbChild = table.childNodes.length;
        for (var i = 0; i < nbChild-2; ++i) {
            table.removeChild(table.lastChild);
        }
    }
}

function clearSelect() {
    var allTests = document.getElementById("allTests");
    if (allTests != null) {
        var nbChild = allTests.childNodes.length;
        for (var i = 0; i < nbChild - 2; ++i) {
            allTests.removeChild(allTests.lastChild);
        }
    }
}

function radioDisplaySuccess()
{
    var radioSF = document.getElementById("succFail");
    var radioS = document.getElementById("succ");
    return (radioSF != null && radioSF.checked) || (radioS != null && radioS.checked);
}

function radioDisplayFail()
{
    var radioSF = document.getElementById("succFail");
    var radioF = document.getElementById("fail");
    return (radioSF != null && radioSF.checked) || (radioF != null && radioF.checked);
}

function extractJsonInformations(data) {
    clearSelect();
    var testNames = new Array();
    var buildNumber = -1;
    var multipleBuilds = false;
    
    for (var i = 0; i < jsonData.length; ++i) {
        var item = jsonData[i];
        var testName = item["TestName"];
        if (testName != null && testNames.indexOf(testName) == -1)
            testNames.push(testName);
            
        var curBuildNumber = item["BuildNumber"];
        multipleBuilds = multipleBuilds || (buildNumber != -1 && buildNumber != curBuildNumber);
        buildNumber = curBuildNumber;
    }
    
    var allTests = document.getElementById("allTests");
    if (allTests != null) {
        for (var i = 0; i < testNames.length; ++i) {
            var name = testNames[i];
            allTests.appendChild(addTestSelect(name, name));
        }
    }
    
    if (testNames.length > 0) {
        var title = document.getElementById("title");
        if (title != null) {
            if (multipleBuilds)
                title.innerHTML = "Multiple builds";
            else
                title.innerHTML = "Build number " + buildNumber;
        }
        
        if (allTests != null) {
            allTests.selectedIndex = 0;
            displayTestResults(null, radioDisplaySuccess(), radioDisplayFail());
        }
    }
}

function displayTestResults(displayTestName, displaySuccess, displayFail) {
    var table = document.getElementById("table");
    if (table != null) {
        clearTable();
        for (var i = 0; i < jsonData.length; ++i) {
            var item = jsonData[i];
            var error = item["Error"];
            
            if (!displayFail && error != 0)
                continue;
            if (!displaySuccess && error == 0)
                continue;
            
            var testName = item["TestName"];
            if (displayTestName == null || displayTestName == testName) {
                var newLine = document.createElement("tr");
                
                var localBuildFolder = USER_FOLDER;
                var localGoldFolder = "";
                if (loadOnServer) {
                    localBuildFolder = BUILD_FOLDER + item["Platform"] + "_" + item["Device"] + "_" + item["Serial"] + "/";
                    localGoldFolder = GOLD_FOLDER+ item["Platform"] + "_" + item["Device"] + "/";
                    
                    if (item["BuildNumber"] > 0)
                        localBuildFolder += item["BuildNumber"] + "/";
                }
                
                var newCellDevice = document.createElement("td");
                newCellDevice.innerHTML = item["Platform"] + " " + item["Device"];
                newLine.appendChild(newCellDevice);
                
                var newCellTestName = document.createElement("td");
                newCellTestName.innerHTML = item["TestName"] + " - frame " + item["FrameIndex"];
                newLine.appendChild(newCellTestName);

                var newCellBuild = document.createElement("td");
                newCellBuild.innerHTML = item["BuildNumber"];
                newLine.appendChild(newCellBuild);
                
                newLine.appendChild(createImageCellElement(GOLD_SERVER + localGoldFolder + item["ComputedImage"]));
                
                if (error != 0) {
                    newLine.appendChild(createImageCellElement(GOLD_SERVER + localBuildFolder + item["ComputedImage"]));
                    newLine.appendChild(createImageCellElement(GOLD_SERVER + localBuildFolder + item["DiffImage"]));
                    newLine.appendChild(createImageCellElement(GOLD_SERVER + localBuildFolder + item["NormDiffImage"]));
                }
                else {
                    newLine.appendChild(createImageCellElement(null));
                    newLine.appendChild(createImageCellElement(null));
                    newLine.appendChild(createImageCellElement(null));
                }
                
                var newCellError = document.createElement("td");            
                newCellError.innerHTML = error;
                if (error == 0) {
                    newCellError.className = "success";
                }
                else {
                    newCellError.className = "fail";
                }
                newLine.appendChild(newCellError);
                
                table.appendChild(newLine);
            }
        }
    }
}

function createImageCellElement(imageName) {
    var newCell = document.createElement("td");
    newCell.className = "imageCell";
    if (imageName == null) {
        newCell.className += " noImage";
    }
    else {
        var img = document.createElement("img");
        img.src = imageName;
        newCell.appendChild(img);
    }
    return newCell;
}

function addTestSelect(testValue, testName) {
    var newOpt = document.createElement("option");
    newOpt.value = testValue;
    newOpt.innerHTML = testName;
    return newOpt;
}

/////////////////////////
// SET EVENTS
/////////////////////////
document.getElementById('files').addEventListener('change', handleFileSelect, false);
document.getElementById("allTests").addEventListener("change", selectTest, false);
document.getElementById("succFail").addEventListener("change", radioChange, false);
document.getElementById("succ").addEventListener("change", radioChange, false);
document.getElementById("fail").addEventListener("change", radioChange, false);
document.getElementById("localLoad").addEventListener("change", loadLocalChange, false);
mergeInto(LibraryManager.library, {
  WebGLInputBridge_Show: function (objectNamePtr, methodNamePtr, initialTextPtr) {
    var objectName = UTF8ToString(objectNamePtr);
    var methodName = UTF8ToString(methodNamePtr);
    var initialText = UTF8ToString(initialTextPtr || 0);
    var inputId = 'unity-webgl-ime-input';
    var input = document.getElementById(inputId);

    if (!input) {
      input = document.createElement('input');
      input.id = inputId;
      input.type = 'text';
      input.autocomplete = 'off';
      input.spellcheck = false;
      input.style.position = 'fixed';
      input.style.left = '50%';
      input.style.bottom = '8%';
      input.style.transform = 'translateX(-50%)';
      input.style.width = 'min(520px, 80vw)';
      input.style.height = '36px';
      input.style.zIndex = '9999';
      input.style.fontSize = '18px';
      input.style.padding = '6px 10px';
      input.style.border = '1px solid rgba(0,0,0,.25)';
      input.style.borderRadius = '6px';
      input.style.background = 'rgba(255,255,255,.96)';
      input.style.color = '#222';
      document.body.appendChild(input);
    }

    input.value = initialText;
    input.style.display = 'block';

    var isComposing = false;

    function sendToUnity(value) {
      if (typeof unityInstance !== 'undefined' && unityInstance) {
        unityInstance.SendMessage(objectName, methodName, value);
      }
    }

    input.oncompositionstart = function () { isComposing = true; };
    input.oncompositionend = function () {
      isComposing = false;
      sendToUnity(input.value);
    };
    input.oninput = function () {
      if (isComposing) return;
      sendToUnity(input.value);
    };
    input.onkeydown = function (event) {
      if (event.key === 'Escape') input.blur();
    };
    setTimeout(function () {
      input.focus();
      input.select();
    }, 0);
  },

  WebGLInputBridge_Hide: function () {
    var input = document.getElementById('unity-webgl-ime-input');
    if (!input) return;
    input.blur();
    input.style.display = 'none';
  }
});

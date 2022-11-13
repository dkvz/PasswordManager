import './css/site.css';
import aes from './aes';
import {
  postLogin,
  postLogout,
  getNames,
  getEntry,
  editEntry,
  createEntry,
  saveData,
  deleteEntry
} from './api';
import { base64ToUint8Array } from './b64Uint8ArrayConversions';
import Toaster from './toaster';
import {
  showSuccessSlide,
  setLoading,
  showModal,
  removeNodesFromElement,
  addHtmlOption
} from './ui';

const loginForm = document.getElementById('loginForm');

if (loginForm) {
  // We're on the login page:
  const state = {
    sequence: [],
    passwordVisible: false
  };

  // Put up loading screen while initializing:
  const loading = document.getElementById('loading');
  setLoading(loading, true);

  // Setup the toaster:
  const notification = document.getElementById('notification');
  state.toaster = new Toaster(notification);

  const seqBtns = document.querySelectorAll('.sequence-grid button');
  const slides = document.querySelectorAll('.slides-container > section');
  const masterPwd = document.getElementById('masterPassword');
  const dataFile = document.getElementById('dataFile');
  const nameSelect = document.getElementById('nameSelect');
  const selectEntryForm = document.getElementById('selectEntryForm');
  const entryDate = document.getElementById('entryDate');
  const nameInput = document.getElementById('nameInput');
  const passwordInput = document.getElementById('passwordInput');
  const hiddenPasswordInput = document.getElementById('hiddenPasswordInput');
  const modalPasswordInput = document.getElementById('modalPasswordInput');
  const passwordModal = document.getElementById('passwordModal');
  const unsavedChanges = document.getElementById('unsavedChanges');
  const showHideBtnImg = document.querySelector('#showHideBtn img');
  const filterInput = document.getElementById('filterInput');
  const sessionIdInput = document.getElementById('sessionId');
  state.sessionId = sessionIdInput ? sessionIdInput.value : '';


  // Cheap debug tactic
  //showSuccessSlide(slides);


  const seqInputEvent = (e) => {
    // Button is in e.currentTarget
    state.sequence.push(
      e.currentTarget.getAttribute('data-x') + ',' +
      e.currentTarget.getAttribute('data-y')
    );
  };

  const showPassword = (show) => {
    state.passwordVisible = show;
    if (show) {
      showHideBtnImg.src = '/images/eye-slash.svg';
      passwordInput.value = hiddenPasswordInput.value;
      passwordInput.type = 'text';
      passwordInput.removeAttribute('placeholder');
    } else {
      showHideBtnImg.src = '/images/eye.svg';
      passwordInput.value = '';
      passwordInput.type = 'password';
      passwordInput.setAttribute('placeholder', 'Password hidden');
    }
  };

  const getSelectedEntry = () => {
    // Don't forget to set the loading status somewhere.
    // Actually it's so fast that I won't for now.
    state.toaster.close();
    state.entryId = 0;
    if (nameSelect.selectedIndex !== undefined && nameSelect.selectedIndex >= 0) {
      const entryId = nameSelect.options[nameSelect.selectedIndex].value;
      getEntry(state.sessionHash, entryId)
        .then(data => {
          // We need to decrypt the password:
          if (data && data.password) {
            aes.decrypt(data.password, state.key)
              .then(pwd => {
                nameInput.value = data.name;
                entryDate.textContent =
                  `Last modified: ${data.parsedDate.toLocaleDateString()}`;
                hiddenPasswordInput.value = pwd;
                showPassword(false);
                state.entryId = entryId;
              })
              .catch(err => {
                state.toaster.error(`Could not decrypt data from server`);
                console.log(err);
              });
          } else state.toaster.error(`Server sent malformed data`);
        })
        .catch(err => {
          state.toaster.error(`Error loading data for the entry: ${err}`);
        })
    }
  };

  const updateEntryList = (names) => {
    removeNodesFromElement(nameSelect);
    nameSelect.value = null;
    names.forEach(
      (e) =>
        addHtmlOption(
          nameSelect,
          e.name,
          document,
          e.index,
          { type: 'dblclick', callback: getSelectedEntry }
        )
    );
  };

  const refreshNames = () => {
    return new Promise((resolve) => {
      filterInput.value = '';
      getNames(state.sessionHash)
        .then(data => {
          state.names = data.map((n, i) => ({ name: n, index: i + 1 }))
            .sort(
              (a, b) =>
                a.name.toLowerCase() < b.name.toLowerCase() ? -1 : 1
            );
          updateEntryList(state.names);
          resolve(true);
        })
        .catch(err => {
          state.toaster.error(`Error loading the entries: ${err}`);
          resolve(false);
        });
    });
  };

  const reset = () => {
    state.sequence = [];
    masterPwd.value = '';
    setLoading(loading, false);
  };

  const resetFields = () => {
    nameInput.value = '';
    passwordInput.value = '';
    hiddenPasswordInput.value = '';
    modalPasswordInput.value = '';
  };

  const showUnsavedChanges = (show) => {
    if (show) unsavedChanges.classList.remove('d-none');
    else unsavedChanges.classList.add('d-none');
  };

  for (let i = 0; i < seqBtns.length; i++) {
    seqBtns[i].addEventListener('click', seqInputEvent);
  }

  loginForm.addEventListener('submit', (e) => {
    e.preventDefault();
    state.toaster.close();
    setLoading(loading, true);
    const seqStr = state.sequence.join(';');
    // Convert sessionId to bytes:
    state.sessionIdBytes = base64ToUint8Array(state.sessionId);
    // We need to SHA1 the sequence, and then SHA1(SHA1(seq) + sessionId)
    // And we need to keep (SHA1(seq) + sessionId) because it's needed to
    // decrypt what the server sends.
    aes.hashStringToBytes(seqStr)
      .then(seqHashBytes => {
        //state.key = seqHash + state.sessionId;
        // Concat the byte arrays into one:
        state.key = new Uint8Array(seqHashBytes.length + state.sessionIdBytes.length);
        state.key.set(seqHashBytes);
        state.key.set(state.sessionIdBytes, seqHashBytes.length);
        aes.hashBytesToString(state.key)
          .then(sessionHash => {
            state.sessionHash = sessionHash;
            // Request actual login from API:
            aes.encrypt(masterPwd.value, state.key)
              .then(encryptedPwd => {
                postLogin(
                  sessionHash,
                  encryptedPwd,
                  dataFile.selectedIndex
                )
                  .then(() => {
                    // Get the entry names:
                    refreshNames().then(() => {
                      setLoading(loading, false);
                      document.title = document.title.replace('Login', 'CONNECTED');
                      showSuccessSlide(slides);
                    });
                  })
                  .catch((err) => {
                    state.toaster.error(`Login error: ${err}`);
                    reset();
                  });
              })
              .catch(err => {
                state.toaster.error(`Encryption error: ${err}`);
                reset();
              });
          });
      });
  });

  document.getElementById('resetBtn').addEventListener('click', () => {
    state.toaster.close();
    reset();
  });

  document.getElementById('logoutBtn').addEventListener('click', () => {
    if (state.sessionHash) {
      postLogout(state.sessionHash)
        .then(resp => {
          console.log(resp);
          window.location.reload();
        });
    }
  });

  filterInput.addEventListener('input', (e) => {
    if (e.currentTarget.value) {
      const filterLowCase = e.currentTarget.value.toLowerCase();
      updateEntryList(
        state.names.filter(n =>
          n.name.toLowerCase().indexOf(filterLowCase) >= 0)
      );
    } else {
      updateEntryList(state.names);
    }
  });


  // Events for the second slide:
  // ----------------------------

  document.getElementById('newBtn').addEventListener('click', () => {
    state.entryId = 0;
    // Focus the name entry field:
    showPassword(false);
    passwordInput.setAttribute('placeholder', 'Enter new password');
    nameInput.value = '';
    nameInput.focus();
    nameInput.scrollIntoView();
    passwordInput.value = '';
    hiddenPasswordInput.value = '';
  });

  document.getElementById('saveBtn').addEventListener('click', () => {
    const name = nameInput.value.trim();
    const pwd = passwordInput.value || hiddenPasswordInput.value;
    state.toaster.close();
    if (name.length > 0 && pwd.length > 0) {
      setLoading(loading, true);
      // Encrypt the password:
      aes.encrypt(pwd, state.key)
        .then(encryptedPwd => {
          // Check state.entryId to see if we're adding or modifying:
          if (state.entryId) {
            // Edit / update mode.
            editEntry(state.sessionHash, state.entryId, name, encryptedPwd)
              .then(refreshNames)
              .then(() => {
                showUnsavedChanges(true);
                setLoading(loading, false);
                state.toaster.info('The entry has been modified');
              });
            // Don't forget to reset state.entryId:
            state.entryId = 0;
          } else {
            // Adding a new entry.
            createEntry(state.sessionHash, name, encryptedPwd)
              .then(refreshNames)
              .then(() => {
                resetFields();
                showUnsavedChanges(true);
                setLoading(loading, false);
                state.toaster.info('The entry has been added');
              })
              .catch((err) => {
                state.toaster.error(`Error attempting to add the entry: ${err}`);
                setLoading(loading, false);
              });
          }
        })
        .catch(() => {
          state.toaster.error('Could not save data - encryption error');
          setLoading(loading, false);
        });
    } else {
      state.toaster.warn('Name or password fields cannot be empty');
    }
  });

  document.getElementById('deleteBtn').addEventListener('click', () => {
    // Check that we got something selected in the list:
    state.toaster.close();
    const entryId = nameSelect.options[nameSelect.selectedIndex].value;
    if (entryId) {
      setLoading(loading, true);
      deleteEntry(state.sessionHash, entryId)
        .then(refreshNames)
        .then(() => {
          resetFields();
          showUnsavedChanges(true);
          setLoading(loading, false);
          state.toaster.info('Entry has been removed');
        })
        .catch((err) => {
          state.toaster.error(`Error attempting to delete the entry: ${err}`);
          setLoading(loading, false);
        });
    } else {
      state.toaster.warn('Please select an item first');
    }
  });

  document.getElementById('saveChangesBtn').addEventListener('click', () => {
    // We need to ask for the master password using the modal.
    modalPasswordInput.value = '';
    showModal(passwordModal, true);
    modalPasswordInput.focus();
  });

  document.getElementById('mPwdForm').addEventListener('submit', (e) => {
    e.preventDefault();
    // Confirm to the server that we want to save the changes.
    // Don't forget to hide the savechanges section thingy in case of success.
    if (modalPasswordInput.value !== undefined && modalPasswordInput.value.length > 0) {
      // Close the password modal:
      showModal(passwordModal, false);
      state.toaster.close();
      setLoading(loading, true);
      aes.encrypt(modalPasswordInput.value, state.key)
        .then(encryptedPwd => {
          saveData(state.sessionHash, encryptedPwd)
            .then(() => {
              showUnsavedChanges(false);
              refreshNames().then(
                () => {
                  state.toaster.info('Data has been saved on the server');
                  setLoading(loading, false);
                }).catch(() => {
                  state.toaster.error('Could not refresh data from the server');
                  setLoading(loading, false);
                });
            })
            .catch(err => {
              state.toaster.error(`Error trying to save data on the server: ${err}`);
              setLoading(loading, false);
            });
        })
        .catch(err => {
          state.toaster.error(`Encryption error: ${err}`);
          setLoading(loading, false);
        });
    }
  });

  document.getElementById('passwordModalCancelBtn').addEventListener('click', () => {
    showModal(passwordModal, false);
  });

  selectEntryForm.addEventListener('submit', (e) => {
    e.preventDefault();
    getSelectedEntry();
  });

  document.getElementById('showHideBtn').addEventListener('click', () =>
    showPassword(!state.passwordVisible)
  );

  document.getElementById('clipboardBtn').addEventListener('click', () => {
    if (navigator.clipboard) {
      if (state.entryId) {
        navigator.clipboard.writeText(
          state.passwordVisible ?
            passwordInput.value :
            hiddenPasswordInput.value
        );
      }
    } else {
      // I actually used alert() :D
      alert('Your browser does\'t have the required clipboard API');
    }
  });

  setLoading(loading, false);

}
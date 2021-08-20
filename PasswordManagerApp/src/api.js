
function createApiRequest(body) {
  return {
    method: 'post',
    headers: {
      'Accept': 'application/json',
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(body)
  };
}

function basicPromiseStatusHandler(status, reject) {
  switch(status) {
    case 403:
      reject('Unauthorized access');
      break;
    default:
      reject(`Server error, status ${resp.status}`);
  }
}

export function postLogin(sessionId, password, dataFile) {
  return new Promise((resolve, reject) => {
    fetch('/api/v1/login', createApiRequest({ sessionId, password, dataFile }))
    .then(resp => { 
      // I think I'm going to respond with JSON.
      // Should probably check the response status here.
      if (resp.status >= 200 && resp.status < 300) {
        resolve();
      } else {
        switch(resp.status) {
          case 400:
            reject('Invalid arguments');
            break;
          case 401:
            reject('Session validation failed. Sequence code is wrong or your session has expired.');
            break;
          case 403:
            reject('Invalid password or data file');
            break;
          default:
            reject(`Server error, status ${resp.status}`);
        }
      }
    })
    .catch(() => reject('Network error'));
  });
}

export function postLogout(sessionId) {
  return new Promise((resolve) => {
    fetch('/api/v1/logout', createApiRequest({ sessionId }))
    .then(resp => {
      // We always expect this to work.
      resolve(resp);
    })
    .catch(err => resolve(err));
  });
}

export function getNames(sessionId) {
  return new Promise((resolve, reject) => {
    fetch('/api/v1/names', createApiRequest({ sessionId }))
    .then(resp => {
      if (resp.status >= 200 && resp.status < 300) {
        resp.json().then(resolve);
      } else 
        basicPromiseStatusHandler(resp.status, reject);
    })
    .catch(reject);
  });
}

/**
 * entryId has to have an index that starts at 1.
 */
export function getEntry(sessionId, entryId) {
  return new Promise((resolve, reject) => {
    fetch('/api/v1/entry', createApiRequest({ sessionId, entryId }))
    .then(resp => {
      if (resp.status >= 200 && resp.status < 300) {
        resp.json()
        .then(data => {
          const pDate = new Date(data.date);
          if (!isNaN(pDate.getTime())) data.parsedDate = pDate;
          resolve(data);
        });
      } else basicPromiseStatusHandler(resp.status, reject);
    })
    .catch(reject);
  });
}

export function deleteEntry(sessionId, entryId) {
  return new Promise((resolve, reject) => {
    fetch('/api/v1/entry', 
      createApiRequest({ sessionId, entryId, operation: 3 })
    )
    .then(resp => {
      if (resp.status >= 200 && resp.status < 300) {
        resolve(resp);
      } else 
        basicPromiseStatusHandler(resp.status, reject);
    })
    .catch(reject);
  });
}

/**
 * The password argument should be encrypted with the
 * session key.
 */
export function createEntry(sessionId, name, password) {
  return new Promise((resolve, reject) => {
    fetch('/api/v1/entry', 
      createApiRequest(
        { 
          sessionId,
          name,
          password,
          operation: 1
        }
      )
    )
    .then(resp => {
      if (resp.status >= 200 && resp.status < 300) {
        resolve(resp);
      } else 
        basicPromiseStatusHandler(resp.status, reject);
    })
    .catch(reject);
  });
}

/**
 * The password argument should be encrypted with the
 * session key.
 */
export function editEntry(sessionId, entryId, name, password) {
  return new Promise((resolve, reject) => {
    fetch('/api/v1/entry', 
      createApiRequest(
        { 
          sessionId,
          entryId,
          name,
          password,
          operation: 2
        }
      )
    )
    .then(resp => {
      if (resp.status >= 200 && resp.status < 300) {
        resolve(resp);
      } else 
        basicPromiseStatusHandler(resp.status, reject);
    })
    .catch(reject);
  });
}

/**
 * The master password should be encrypted with the
 * session key.
 * 
 * The endpoint gives a 401 error i ncase the given
 * master password is not the original one used to
 * encrypt the file.
 */
export function saveData(sessionId, masterPassword) {
  return new Promise((resolve, reject) => {
    fetch('/api/v1/save', 
      createApiRequest({ sessionId, password: masterPassword })
    )
    .then(resp => {
      if (resp.status >= 200 && resp.status < 300) {
        resolve(resp);
      } else {
        switch(resp.status) {
          case 401:
            reject('The master password given is different from the original master password');
            break;
          case 403:
            reject('Unauthorized attempt at saving the data');
            break;
          default:
            reject(`Server error, status ${resp.status}`);
        }
      } 
    })
    .catch(reject);
  });
}
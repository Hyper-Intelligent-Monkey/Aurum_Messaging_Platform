import { computed } from 'vue';
import { required, email, minLength, maxLength, sameAs, helpers } from '@vuelidate/validators';

const req = (fieldName) => helpers.withMessage(`${fieldName} is required`, required);

const emailRule = helpers.withMessage("Must be a valid email", email);
const minLen8 = minLength(8);
const validUsernameChars = helpers.withMessage(
  "Username can only contain letters, numbers, and underscores",
  (value) => !value || /^[a-zA-Z0-9_]+$/.test(value)
);
const minLen3 = helpers.withMessage(
  "Username must be at least 3 letters or numbers",
  (value) => !value || value.trim().replace(/^_+|_+$/g, '').length >= 3
);
const maxLen20 = helpers.withMessage(
  "Username cannot exceed 20 characters",
  (value) => !value || value.trim().replace(/^_+|_+$/g, '').length <= 20
);

const commonRules = {
  email: {
    required: req("Email"),
    email: emailRule,
  },
  password: {
    required: req("Password"),
    minLength: minLen8,
  },
};

// login parameters
export const getLoginRules = () => commonRules;

// username rules
export const getUsernameRules = () => ({
  required: req("Username"),
  validChars: validUsernameChars,
  minLength: minLen3,
  maxLength: maxLen20,
});

// register parameters
export const getRegisterRules = (formData) => ({
  ...commonRules,
  username: getUsernameRules(),
  confirmPassword: {
    required: req("Confirm password"),
    sameAs: helpers.withMessage("Passwords do not match", sameAs(computed(() => formData.password))
    ),
  },
});


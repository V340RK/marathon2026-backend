import { HttpErrorResponse } from '@angular/common/http';
import { getServerErrorMessage } from './http-error-helpers';

describe('getServerErrorMessage', () => {
  const cases: { name: string; body: unknown; expected: string }[] = [
    { name: 'reads a plain string body', body: 'Plain error', expected: 'Plain error' },
    { name: 'reads the error field', body: { error: 'Error field' }, expected: 'Error field' },
    { name: 'reads the message field', body: { message: 'Message field' }, expected: 'Message field' },
    {
      name: 'prefers the error field over message',
      body: { error: 'Error field', message: 'Message field' },
      expected: 'Error field',
    },
    { name: 'returns an empty string for an empty body', body: null, expected: '' },
  ];

  cases.forEach(({ name, body, expected }) => {
    it(name, () => {
      const error = new HttpErrorResponse({ error: body, status: 400 });

      expect(getServerErrorMessage(error)).toBe(expected);
    });
  });
});

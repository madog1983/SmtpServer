﻿using System;
using System.Buffers;

namespace SmtpServer.Text
{
    public ref struct TokenReader
    {
        readonly ReadOnlySequence<char> _buffer;
        Token _peek;
        bool _hasPeeked;
        SequencePosition _position;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        public TokenReader(ReadOnlySequence<char> buffer)
        {
            _buffer = buffer;
            _position = buffer.GetPosition(0);
            _peek = default;
            _hasPeeked = false;
        }

        /// <summary>
        /// Peek at the next token in the sequence.
        /// </summary>
        /// <returns>The next token in the sequence.</returns>
        public Token Peek()
        {
            if (_hasPeeked == false)
            {
                _peek = ConsumeToken();
                _hasPeeked = true;
            }

            return _peek;
        }

        /// <summary>
        /// Take the next token from the sequence.
        /// </summary>
        /// <returns>The next token from the sequence.</returns>
        public Token Take()
        {
            if (_hasPeeked)
            {
                _hasPeeked = false;
                return _peek;
            }

            return ConsumeToken();
        }

        /// <summary>
        /// Consume the next token from the buffer.
        /// </summary>
        /// <returns>The token that was read and consumed from the buffer.</returns>
        Token ConsumeToken()
        {
            var token = ReadToken();

            _position = _buffer.GetPosition(token.Text.Length, _position);

            return token;
        }

        /// <summary>
        /// Read a token from the current position in the sequence.
        /// </summary>
        /// <returns>The token that was read from the sequence.</returns>
        Token ReadToken()
        {
            if (_position.Equals(_buffer.End) || _buffer.TryGet(ref _position, out var memory, false) == false)
            {
                return default;
            }

            var span = memory.Span;
            switch (span[0])
            {
                case { } ch when Token.IsText(ch):
                    return new Token(TokenKind.Text, ReadWhile(ref span, Token.IsText));

                case { } ch when Token.IsNumber(ch):
                    return new Token(TokenKind.Number, ReadWhile(ref span, Token.IsNumber));

                case { } ch when Token.IsWhiteSpace(ch):
                    return new Token(TokenKind.Space, ReadOne(ref span));
            }

            return new Token(TokenKind.Other, ReadOne(ref span));
        }

        static ReadOnlySpan<char> ReadWhile(ref ReadOnlySpan<char> span, Func<char, bool> predicate)
        {
            var count = 0;
            while (count < span.Length && predicate(span[count]))
            {
                count++;
            }

            return span.Slice(0, count);
        }

        static ReadOnlySpan<char> ReadOne(ref ReadOnlySpan<char> span)
        {
            return span.Slice(0, 1);
        }

        //public long Checkpoint()
        //{
        //    return _reader.Consumed;
        //}

        //public void Restore(long checkpoint)
        //{
        //    var count = _reader.Consumed - checkpoint;

        //    if (count > 0)
        //    {
        //        _reader.Rewind(count);
        //        _hasPeeked = false;
        //    }
        //}

        //public ReadOnlySequence<char> Slice(long from, long to)
        //{
        //    return _reader.Sequence.Slice(from, to - from);
        //}
    }
}
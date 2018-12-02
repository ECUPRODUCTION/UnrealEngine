/*
 *  Copyright (c) 2014 The WebRTC project authors. All Rights Reserved.
 *
 *  Use of this source code is governed by a BSD-style license
 *  that can be found in the LICENSE file in the root of the source
 *  tree. An additional intellectual property rights grant can be found
 *  in the file PATENTS.  All contributing project authors may
 *  be found in the AUTHORS file in the root of the source tree.
 */

#ifndef WEBRTC_MODULES_AUDIO_CODING_CODECS_G722_INCLUDE_AUDIO_ENCODER_G722_H_
#define WEBRTC_MODULES_AUDIO_CODING_CODECS_G722_INCLUDE_AUDIO_ENCODER_G722_H_

#include "webrtc/base/buffer.h"
#include "webrtc/base/scoped_ptr.h"
#include "webrtc/modules/audio_coding/codecs/audio_encoder.h"
#include "webrtc/modules/audio_coding/codecs/audio_encoder_mutable_impl.h"
#include "webrtc/modules/audio_coding/codecs/g722/include/g722_interface.h"

namespace webrtc {

class AudioEncoderG722 final : public AudioEncoder {
 public:
  struct Config {
    Config() : payload_type(9), frame_size_ms(20), num_channels(1) {}
    bool IsOk() const;

    int payload_type;
    int frame_size_ms;
    int num_channels;
  };

  explicit AudioEncoderG722(const Config& config);
  ~AudioEncoderG722() override;

  int SampleRateHz() const override;
  int NumChannels() const override;
  size_t MaxEncodedBytes() const override;
  int RtpTimestampRateHz() const override;
  size_t Num10MsFramesInNextPacket() const override;
  size_t Max10MsFramesInAPacket() const override;
  int GetTargetBitrate() const override;
  EncodedInfo EncodeInternal(uint32_t rtp_timestamp,
                             const int16_t* audio,
                             size_t max_encoded_bytes,
                             uint8_t* encoded) override;

 private:
  // The encoder state for one channel.
  struct EncoderState {
    G722EncInst* encoder;
    rtc::scoped_ptr<int16_t[]> speech_buffer;   // Queued up for encoding.
    rtc::Buffer encoded_buffer;                 // Already encoded.
    EncoderState();
    ~EncoderState();
  };

  size_t SamplesPerChannel() const;

  const int num_channels_;
  const int payload_type_;
  const size_t num_10ms_frames_per_packet_;
  size_t num_10ms_frames_buffered_;
  uint32_t first_timestamp_in_buffer_;
  const rtc::scoped_ptr<EncoderState[]> encoders_;
  rtc::Buffer interleave_buffer_;
};

struct CodecInst;

class AudioEncoderMutableG722
    : public AudioEncoderMutableImpl<AudioEncoderG722> {
 public:
  explicit AudioEncoderMutableG722(const CodecInst& codec_inst);
};

}  // namespace webrtc
#endif  // WEBRTC_MODULES_AUDIO_CODING_CODECS_G722_INCLUDE_AUDIO_ENCODER_G722_H_
